using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using ERezeptExtractor.Models;
using Microsoft.Extensions.Logging;

namespace ERezeptExtractor.Serialization
{
    /// <summary>
    /// FHIR XML extractor based on AVD specifications with advanced business logic support
    /// </summary>
    public class AVDFhirExtractor
    {
        private readonly ILogger<AVDFhirExtractor> _logger;
        private readonly Dictionary<string, string> _namespaces;

        public AVDFhirExtractor(ILogger<AVDFhirExtractor>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AVDFhirExtractor>.Instance;
            
            // Standard FHIR namespaces
            _namespaces = new Dictionary<string, string>
            {
                { "fhir", "http://hl7.org/fhir" },
                { "xsi", "http://www.w3.org/2001/XMLSchema-instance" }
            };
        }

        /// <summary>
        /// Extracts data from FHIR XML based on AVD specifications
        /// </summary>
        /// <param name="fhirXml">FHIR XML document</param>
        /// <param name="specifications">AVD specifications to apply</param>
        /// <returns>Extracted data container</returns>
        public AVDExtractedData ExtractData(XmlDocument fhirXml, List<AVDSpecification> specifications)
        {
            var result = new AVDExtractedData();
            
            try
            {
                _logger.LogInformation("Starting AVD FHIR extraction with {SpecCount} specifications", 
                    specifications.Count);

                // Setup namespace manager
                var namespaceManager = CreateNamespaceManager(fhirXml);

                // Process each specification
                foreach (var spec in specifications)
                {
                    try
                    {
                        var extractedValue = ExtractAttributeValue(fhirXml, spec, namespaceManager, result);
                        result.SetValue(spec.Attribut, extractedValue);
                        
                        if (extractedValue != null)
                        {
                            _logger.LogDebug("Extracted {Attribut}: {Value}", spec.Attribut, extractedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Failed to extract attribute '{spec.Attribut}' (ID: {spec.ID}): {ex.Message}";
                        result.Errors.Add(errorMsg);
                        _logger.LogWarning(ex, "Extraction failed for attribute: {Attribut}", spec.Attribut);
                    }
                }

                // Apply post-processing business logic
                ApplyPostProcessingLogic(result, specifications);

                _logger.LogInformation("AVD extraction completed. Extracted {Count} values, {ErrorCount} errors, {WarningCount} warnings",
                    result.ExtractedValues.Count, result.Errors.Count, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Critical extraction error: {ex.Message}");
                _logger.LogError(ex, "Critical error during AVD FHIR extraction");
            }

            return result;
        }

        private object? ExtractAttributeValue(XmlDocument fhirXml, AVDSpecification spec, 
            XmlNamespaceManager namespaceManager, AVDExtractedData context)
        {
            // Handle special cases with no XPath (like 'spr')
            if (string.IsNullOrEmpty(spec.PartiellerXPath))
            {
                context.Warnings.Add($"No XPath provided for attribute '{spec.Attribut}' - returning null");
                return null;
            }

            // Handle conditional logic specifications
            if (spec.BusinessRules.Any() || spec.PartiellerXPath.Contains("{{"))
            {
                return ExtractWithBusinessLogic(fhirXml, spec, namespaceManager, context);
            }

            // Handle multiple XPath expressions
            if (spec.XPathExpressions.Count > 1)
            {
                return ExtractWithMultipleXPaths(fhirXml, spec, namespaceManager);
            }

            // Simple single XPath extraction
            if (spec.XPathExpressions.Count == 1)
            {
                return ExtractWithSingleXPath(fhirXml, spec.XPathExpressions[0], namespaceManager, spec);
            }

            // Fallback to raw XPath processing
            return ExtractWithRawXPath(fhirXml, spec.PartiellerXPath, namespaceManager, spec);
        }

        private object? ExtractWithBusinessLogic(XmlDocument fhirXml, AVDSpecification spec, 
            XmlNamespaceManager namespaceManager, AVDExtractedData context)
        {
            switch (spec.Attribut)
            {
                case "la_nr":
                    return ExtractLANRWithLogic(fhirXml, namespaceManager, context);
                
                case "la_nr_v":
                    return ExtractLANRVWithLogic(fhirXml, namespaceManager, context);
                
                case "bs_nr":
                    return ExtractBSNRWithLogic(fhirXml, namespaceManager);
                
                default:
                    // Generic business logic handler
                    return ExtractWithGenericBusinessLogic(fhirXml, spec, namespaceManager);
            }
        }

        private object? ExtractLANRWithLogic(XmlDocument fhirXml, XmlNamespaceManager namespaceManager, 
            AVDExtractedData context)
        {
            // Try to get LANR from assistant (Type 03) first
            var assistantXPath = @"//fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and fhir:code/@value='03']/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value";
            
            var assistantLANR = ExtractWithSingleXPath(fhirXml, assistantXPath, namespaceManager, null);
            if (!string.IsNullOrEmpty(assistantLANR?.ToString()))
            {
                context.SetValue("__assistant_found", true); // For la_nr_v logic
                return assistantLANR;
            }

            // Fallback to responsible person (Type 00 or 04)
            var responsibleXPath = @"//fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and (fhir:code/@value='00' or fhir:code/@value='04')]/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value";
            
            var responsibleLANR = ExtractWithSingleXPath(fhirXml, responsibleXPath, namespaceManager, null);
            if (!string.IsNullOrEmpty(responsibleLANR?.ToString()))
            {
                return responsibleLANR;
            }

            // Default value when no LANR found
            return "000000000";
        }

        private object? ExtractLANRVWithLogic(XmlDocument fhirXml, XmlNamespaceManager namespaceManager, 
            AVDExtractedData context)
        {
            // Only populate la_nr_v if assistant was found in la_nr
            if (context.ExtractedValues.ContainsKey("__assistant_found") && 
                (bool)context.ExtractedValues["__assistant_found"])
            {
                // Extract responsible person LANR (Type 00 or 04)
                var responsibleXPath = @"//fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and (fhir:code/@value='00' or fhir:code/@value='04')]/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value";
                
                return ExtractWithSingleXPath(fhirXml, responsibleXPath, namespaceManager, null);
            }

            // Otherwise, leave empty
            return null;
        }

        private object? ExtractBSNRWithLogic(XmlDocument fhirXml, XmlNamespaceManager namespaceManager)
        {
            // Try doctor BSNR first
            var doctorXPath = @"//fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_BSNR']/fhir:value";
            var doctorBSNR = ExtractWithSingleXPath(fhirXml, doctorXPath, namespaceManager, null);
            if (!string.IsNullOrEmpty(doctorBSNR?.ToString()))
            {
                return doctorBSNR;
            }

            // Try hospital IK
            var hospitalXPath = @"//fhir:identifier[fhir:system/@value='http://fhir.de/sid/arge-ik/iknr' or fhir:system/@value='http://fhir.de/NamingSystem/arge-ik/iknr']/fhir:value";
            var hospitalIK = ExtractWithSingleXPath(fhirXml, hospitalXPath, namespaceManager, null);
            if (!string.IsNullOrEmpty(hospitalIK?.ToString()))
            {
                return hospitalIK;
            }

            // Default value
            return "0";
        }

        private object? ExtractWithGenericBusinessLogic(XmlDocument fhirXml, AVDSpecification spec, 
            XmlNamespaceManager namespaceManager)
        {
            // Extract each XPath and return the first non-empty result
            foreach (var xpath in spec.XPathExpressions)
            {
                var result = ExtractWithSingleXPath(fhirXml, xpath, namespaceManager, spec);
                if (!string.IsNullOrEmpty(result?.ToString()))
                {
                    return result;
                }
            }

            return null;
        }

        private object? ExtractWithMultipleXPaths(XmlDocument fhirXml, AVDSpecification spec, 
            XmlNamespaceManager namespaceManager)
        {
            // Try each XPath until we find a value
            foreach (var xpath in spec.XPathExpressions)
            {
                var result = ExtractWithSingleXPath(fhirXml, xpath, namespaceManager, spec);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private object? ExtractWithSingleXPath(XmlDocument fhirXml, string xpath, 
            XmlNamespaceManager namespaceManager, AVDSpecification? spec)
        {
            try
            {
                // Clean up XPath expression
                var cleanXPath = CleanXPath(xpath);
                
                // Select node(s)
                var nodes = fhirXml.SelectNodes(cleanXPath, namespaceManager);
                
                if (nodes != null && nodes.Count > 0)
                {
                    var firstNode = nodes[0];
                    
                    // Extract value based on node type
                    string? value = firstNode.NodeType == XmlNodeType.Attribute 
                        ? firstNode.Value 
                        : firstNode.InnerText;

                    // Apply data type conversion if specification is provided
                    if (spec != null && !string.IsNullOrEmpty(value))
                    {
                        return ConvertToDataType(value, spec);
                    }

                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "XPath execution failed for: {XPath}", xpath);
            }

            return null;
        }

        private object? ExtractWithRawXPath(XmlDocument fhirXml, string rawXPath, 
            XmlNamespaceManager namespaceManager, AVDSpecification spec)
        {
            // Remove business logic comments and try to extract XPath
            var cleanedXPath = Regex.Replace(rawXPath, @"\{\{[^}]+\}\}", string.Empty);
            var xpathLines = cleanedXPath
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && line.Contains("fhir:"))
                .ToList();

            foreach (var xpath in xpathLines)
            {
                var result = ExtractWithSingleXPath(fhirXml, xpath, namespaceManager, spec);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private string CleanXPath(string xpath)
        {
            // Remove extra whitespace and normalize
            return Regex.Replace(xpath.Trim(), @"\s+", " ");
        }

        private object? ConvertToDataType(string value, AVDSpecification spec)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            try
            {
                switch (spec.Darstellung?.ToLower())
                {
                    case "numerisch":
                        if (long.TryParse(value, out long longValue))
                            return longValue;
                        break;
                    
                    case "alphanumerisch":
                    default:
                        return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Data type conversion failed for {Attribut}: {Value}", 
                    spec.Attribut, value);
            }

            return value;
        }

        private void ApplyPostProcessingLogic(AVDExtractedData result, List<AVDSpecification> specifications)
        {
            // Remove internal helper values
            result.ExtractedValues.Remove("__assistant_found");

            // Apply any additional post-processing rules here
            ValidateRequiredFields(result, specifications);
            ApplyDefaultValues(result, specifications);
        }

        private void ValidateRequiredFields(AVDExtractedData result, List<AVDSpecification> specifications)
        {
            var requiredSpecs = specifications.Where(s => !s.IsOptional).ToList();
            
            foreach (var spec in requiredSpecs)
            {
                var value = result.GetValue<string>(spec.Attribut);
                if (string.IsNullOrEmpty(value))
                {
                    result.Warnings.Add($"Required field '{spec.Attribut}' is empty");
                }
            }
        }

        private void ApplyDefaultValues(AVDExtractedData result, List<AVDSpecification> specifications)
        {
            foreach (var spec in specifications)
            {
                var value = result.GetValue<string>(spec.Attribut);
                if (string.IsNullOrEmpty(value))
                {
                    // Apply known default values
                    var defaultValue = GetDefaultValue(spec);
                    if (defaultValue != null)
                    {
                        result.SetValue(spec.Attribut, defaultValue);
                    }
                }
            }
        }

        private string? GetDefaultValue(AVDSpecification spec)
        {
            return spec.Attribut switch
            {
                "la_nr" => "000000000", // Default when no LANR found
                "bs_nr" => "0",         // Default BSNR
                "pat_geb" => "000000",  // Default birth date
                _ => null
            };
        }

        private XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
        {
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            foreach (var ns in _namespaces)
            {
                nsmgr.AddNamespace(ns.Key, ns.Value);
            }
            return nsmgr;
        }
    }
}
