using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ERezeptExtractor.Models;
using ERezeptExtractor.Serialization;
using Microsoft.Extensions.Logging;

namespace ERezeptExtractor
{
    /// <summary>
    /// Main facade for AVD-specification-based FHIR extraction
    /// Orchestrates the parsing of specifications and extraction of data
    /// </summary>
    public class AVDBasedExtractor
    {
        private readonly ILogger<AVDBasedExtractor> _logger;
        private readonly AVDSpecificationParser _specParser;
        private readonly AVDFhirExtractor _fhirExtractor;

        public AVDBasedExtractor(ILogger<AVDBasedExtractor>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AVDBasedExtractor>.Instance;
            _specParser = new AVDSpecificationParser(_logger as ILogger<AVDSpecificationParser>);
            _fhirExtractor = new AVDFhirExtractor(_logger as ILogger<AVDFhirExtractor>);
        }

        /// <summary>
        /// Extracts data from FHIR XML file using AVD specifications
        /// </summary>
        /// <param name="fhirXmlPath">Path to FHIR XML file</param>
        /// <param name="specificationCsvPath">Path to AVD specification CSV file</param>
        /// <returns>Extracted data with validation results</returns>
        public AVDExtractionResult ExtractFromFile(string fhirXmlPath, string specificationCsvPath)
        {
            try
            {
                _logger.LogInformation("Starting AVD-based extraction from {FhirFile} using specs {SpecFile}", 
                    fhirXmlPath, specificationCsvPath);

                // Validate input files
                if (!File.Exists(fhirXmlPath))
                    throw new FileNotFoundException($"FHIR XML file not found: {fhirXmlPath}");
                
                if (!File.Exists(specificationCsvPath))
                    throw new FileNotFoundException($"Specification CSV file not found: {specificationCsvPath}");

                // Parse specifications
                var specifications = _specParser.ParseSpecificationFile(specificationCsvPath);
                _logger.LogInformation("Loaded {SpecCount} specifications", specifications.Count);

                // Validate specifications
                var specIssues = _specParser.ValidateSpecifications(specifications);
                if (specIssues.Count > 0)
                {
                    _logger.LogWarning("Specification validation issues found: {Issues}", 
                        string.Join("; ", specIssues));
                }

                // Load FHIR XML
                var fhirDoc = new XmlDocument();
                fhirDoc.Load(fhirXmlPath);

                // Extract data
                var extractedData = _fhirExtractor.ExtractData(fhirDoc, specifications);

                // Create result
                var result = new AVDExtractionResult
                {
                    ExtractedData = extractedData,
                    Specifications = specifications,
                    SpecificationIssues = specIssues,
                    ExtractionSuccess = extractedData.Errors.Count == 0
                };

                // Perform validation
                var validationResult = extractedData.ValidateData(specifications);
                result.ValidationResult = validationResult;

                _logger.LogInformation("AVD extraction completed. Success: {Success}, Values: {ValueCount}, Errors: {ErrorCount}", 
                    result.ExtractionSuccess, extractedData.ExtractedValues.Count, extractedData.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AVD extraction failed");
                return new AVDExtractionResult
                {
                    ExtractedData = new AVDExtractedData(),
                    ExtractionSuccess = false,
                    SpecificationIssues = new List<string> { $"Extraction failed: {ex.Message}" },
                    Specifications = new List<AVDSpecification>()
                };
            }
        }

        /// <summary>
        /// Extracts data from FHIR XML string using AVD specifications
        /// </summary>
        /// <param name="fhirXmlContent">FHIR XML content</param>
        /// <param name="specificationCsvPath">Path to AVD specification CSV file</param>
        /// <returns>Extracted data with validation results</returns>
        public AVDExtractionResult ExtractFromXml(string fhirXmlContent, string specificationCsvPath)
        {
            try
            {
                _logger.LogInformation("Starting AVD-based extraction from XML content using specs {SpecFile}", 
                    specificationCsvPath);

                // Parse specifications
                var specifications = _specParser.ParseSpecificationFile(specificationCsvPath);

                // Load FHIR XML from string
                var fhirDoc = new XmlDocument();
                fhirDoc.LoadXml(fhirXmlContent);

                // Extract data
                var extractedData = _fhirExtractor.ExtractData(fhirDoc, specifications);

                // Create and return result
                return new AVDExtractionResult
                {
                    ExtractedData = extractedData,
                    Specifications = specifications,
                    SpecificationIssues = _specParser.ValidateSpecifications(specifications),
                    ExtractionSuccess = extractedData.Errors.Count == 0,
                    ValidationResult = extractedData.ValidateData(specifications)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AVD extraction from XML content failed");
                return new AVDExtractionResult
                {
                    ExtractedData = new AVDExtractedData(),
                    ExtractionSuccess = false,
                    SpecificationIssues = new List<string> { $"Extraction failed: {ex.Message}" },
                    Specifications = new List<AVDSpecification>()
                };
            }
        }

        /// <summary>
        /// Gets specifications grouped by FHIR profile for analysis
        /// </summary>
        /// <param name="specificationCsvPath">Path to specification CSV</param>
        /// <returns>Specifications grouped by FHIR profile</returns>
        public Dictionary<string, List<AVDSpecification>> GetSpecificationsByProfile(string specificationCsvPath)
        {
            var specifications = _specParser.ParseSpecificationFile(specificationCsvPath);
            return _specParser.GroupSpecificationsByProfile(specifications);
        }

        /// <summary>
        /// Gets specifications that require complex business logic
        /// </summary>
        /// <param name="specificationCsvPath">Path to specification CSV</param>
        /// <returns>Complex specifications requiring special handling</returns>
        public List<AVDSpecification> GetComplexSpecifications(string specificationCsvPath)
        {
            var specifications = _specParser.ParseSpecificationFile(specificationCsvPath);
            return _specParser.GetComplexSpecifications(specifications);
        }

        /// <summary>
        /// Validates and reports on specification quality
        /// </summary>
        /// <param name="specificationCsvPath">Path to specification CSV</param>
        /// <returns>Validation report</returns>
        public AVDSpecificationReport ValidateSpecifications(string specificationCsvPath)
        {
            try
            {
                var specifications = _specParser.ParseSpecificationFile(specificationCsvPath);
                var issues = _specParser.ValidateSpecifications(specifications);
                var complexSpecs = _specParser.GetComplexSpecifications(specifications);
                var profileGroups = _specParser.GroupSpecificationsByProfile(specifications);

                return new AVDSpecificationReport
                {
                    TotalSpecifications = specifications.Count,
                    ValidationIssues = issues,
                    ComplexSpecifications = complexSpecs.Count,
                    ProfileGroups = profileGroups.Keys.ToList(),
                    Success = issues.Count == 0
                };
            }
            catch (Exception ex)
            {
                return new AVDSpecificationReport
                {
                    Success = false,
                    ValidationIssues = new List<string> { $"Validation failed: {ex.Message}" }
                };
            }
        }
    }

    /// <summary>
    /// Complete result of AVD-based extraction
    /// </summary>
    public class AVDExtractionResult
    {
        public AVDExtractedData ExtractedData { get; set; } = new AVDExtractedData();
        public List<AVDSpecification> Specifications { get; set; } = new List<AVDSpecification>();
        public List<string> SpecificationIssues { get; set; } = new List<string>();
        public bool ExtractionSuccess { get; set; }
        public System.ComponentModel.DataAnnotations.ValidationResult? ValidationResult { get; set; }

        /// <summary>
        /// Gets a summary of the extraction results
        /// </summary>
        public string GetSummary()
        {
            return $"Extraction: {(ExtractionSuccess ? "Success" : "Failed")}, " +
                   $"Values: {ExtractedData.ExtractedValues.Count}, " +
                   $"Errors: {ExtractedData.Errors.Count}, " +
                   $"Warnings: {ExtractedData.Warnings.Count}, " +
                   $"Specs: {Specifications.Count}";
        }
    }

    /// <summary>
    /// Report on specification quality and complexity
    /// </summary>
    public class AVDSpecificationReport
    {
        public int TotalSpecifications { get; set; }
        public List<string> ValidationIssues { get; set; } = new List<string>();
        public int ComplexSpecifications { get; set; }
        public List<string> ProfileGroups { get; set; } = new List<string>();
        public bool Success { get; set; }

        public string GetSummary()
        {
            return $"Total: {TotalSpecifications}, Complex: {ComplexSpecifications}, " +
                   $"Profiles: {ProfileGroups.Count}, Issues: {ValidationIssues.Count}";
        }
    }
}
