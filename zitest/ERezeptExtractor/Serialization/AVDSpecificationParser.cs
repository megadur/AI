using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using ERezeptExtractor.Models;
using Microsoft.Extensions.Logging;

namespace ERezeptExtractor.Serialization
{
    /// <summary>
    /// Parses AVD specification CSV files and converts them to structured specifications
    /// </summary>
    public class AVDSpecificationParser
    {
        private readonly ILogger<AVDSpecificationParser> _logger;

        public AVDSpecificationParser(ILogger<AVDSpecificationParser>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AVDSpecificationParser>.Instance;
        }

        /// <summary>
        /// Parses an AVD specification CSV file
        /// </summary>
        /// <param name="csvFilePath">Path to the CSV file</param>
        /// <returns>List of parsed specifications</returns>
        public List<AVDSpecification> ParseSpecificationFile(string csvFilePath)
        {
            try
            {
                _logger.LogInformation("Parsing AVD specification file: {FilePath}", csvFilePath);
                
                if (!File.Exists(csvFilePath))
                {
                    throw new FileNotFoundException($"Specification file not found: {csvFilePath}");
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null, // Ignore missing fields
                    BadDataFound = null, // Ignore bad data
                    TrimOptions = TrimOptions.Trim
                };

                using var reader = new StringReader(File.ReadAllText(csvFilePath));
                using var csv = new CsvReader(reader, config);
                
                var specifications = new List<AVDSpecification>();
                
                // Read header
                csv.Read();
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    try
                    {
                        var spec = ParseSpecificationRow(csv);
                        if (spec != null && !string.IsNullOrEmpty(spec.Attribut))
                        {
                            specifications.Add(spec);
                            _logger.LogDebug("Parsed specification for attribute: {Attribut} (ID: {ID})", 
                                spec.Attribut, spec.ID);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse CSV row at line {LineNumber}", 
                            csv.Parser.Row);
                    }
                }

                _logger.LogInformation("Successfully parsed {Count} specifications", specifications.Count);
                return specifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AVD specification file: {FilePath}", csvFilePath);
                throw;
            }
        }

        private AVDSpecification? ParseSpecificationRow(CsvReader csv)
        {
            var spec = new AVDSpecification();

            // Parse basic fields with safe field access
            spec.Attribut = GetFieldValue(csv, "Attribut") ?? string.Empty;
            
            // Skip rows without an attribute name (continuation rows or empty rows)
            if (string.IsNullOrWhiteSpace(spec.Attribut))
            {
                return null;
            }

            // Parse ID field
            var idField = GetFieldValue(csv, "ID ");
            if (int.TryParse(idField?.Trim(), out int id))
            {
                spec.ID = id;
            }

            spec.BeschreibungDesAttributs = GetFieldValue(csv, "Beschreibung des Attributs") ?? string.Empty;
            spec.Laenge = GetFieldValue(csv, "Länge") ?? string.Empty;
            spec.Darstellung = GetFieldValue(csv, "Darstellung") ?? string.Empty;
            spec.Profile = GetFieldValue(csv, "Profile") ?? string.Empty;
            spec.AnpassungNotwendigZusaetzl = GetFieldValue(csv, "Anpassung notwendig Zusätzl.") ?? string.Empty;
            spec.PartiellerXPath = GetFieldValue(csv, "Partieller XPath") ?? string.Empty;

            // Parse business rules and XPath expressions
            ParseBusinessRulesAndXPaths(spec);

            return spec;
        }

        private string? GetFieldValue(CsvReader csv, string fieldName)
        {
            try
            {
                return csv.GetField(fieldName);
            }
            catch
            {
                // Field might not exist or be accessible
                return null;
            }
        }

        private void ParseBusinessRulesAndXPaths(AVDSpecification spec)
        {
            if (string.IsNullOrEmpty(spec.PartiellerXPath))
                return;

            // Extract business rules from comments (text within {{ and }})
            var businessRulePattern = @"\{\{([^}]+)\}\}";
            var businessRuleMatches = Regex.Matches(spec.PartiellerXPath, businessRulePattern);
            
            foreach (Match match in businessRuleMatches)
            {
                var rule = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(rule))
                {
                    spec.BusinessRules.Add(rule);
                }
            }

            // Extract XPath expressions (remove comments and clean up)
            var xpathText = Regex.Replace(spec.PartiellerXPath, businessRulePattern, string.Empty);
            
            // Split by lines and clean up
            var xpathLines = xpathText
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && IsValidXPathFragment(line));

            spec.XPathExpressions.AddRange(xpathLines);
        }

        private bool IsValidXPathFragment(string xpath)
        {
            // Basic validation for XPath fragments
            if (string.IsNullOrWhiteSpace(xpath))
                return false;

            // Should contain typical XPath elements
            return xpath.Contains("fhir:") || 
                   xpath.Contains("@value") || 
                   xpath.Contains("[") ||
                   xpath.StartsWith("//");
        }

        /// <summary>
        /// Groups specifications by their FHIR profile for organized processing
        /// </summary>
        public Dictionary<string, List<AVDSpecification>> GroupSpecificationsByProfile(
            List<AVDSpecification> specifications)
        {
            var grouped = new Dictionary<string, List<AVDSpecification>>();

            foreach (var spec in specifications)
            {
                if (string.IsNullOrEmpty(spec.Profile))
                    continue;

                // Handle multiple profiles separated by newlines
                var profiles = spec.Profile
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p));

                foreach (var profile in profiles)
                {
                    if (!grouped.ContainsKey(profile))
                    {
                        grouped[profile] = new List<AVDSpecification>();
                    }
                    grouped[profile].Add(spec);
                }
            }

            return grouped;
        }

        /// <summary>
        /// Gets specifications that require special business logic handling
        /// </summary>
        public List<AVDSpecification> GetComplexSpecifications(List<AVDSpecification> specifications)
        {
            return specifications
                .Where(spec => 
                    spec.BusinessRules.Any() || 
                    spec.RequiresAdaptation ||
                    spec.XPathExpressions.Count > 1 ||
                    spec.PartiellerXPath.Contains("falls") || // German conditional logic
                    spec.PartiellerXPath.Contains("wenn") ||  // German conditional logic
                    spec.PartiellerXPath.Contains("ansonsten")) // German conditional logic
                .ToList();
        }

        /// <summary>
        /// Validates the parsed specifications for completeness
        /// </summary>
        public List<string> ValidateSpecifications(List<AVDSpecification> specifications)
        {
            var issues = new List<string>();

            foreach (var spec in specifications)
            {
                if (string.IsNullOrEmpty(spec.Attribut))
                {
                    issues.Add($"Specification with ID {spec.ID} has no attribute name");
                }

                if (spec.ID <= 0)
                {
                    issues.Add($"Specification '{spec.Attribut}' has invalid ID: {spec.ID}");
                }

                if (string.IsNullOrEmpty(spec.PartiellerXPath) && spec.Attribut != "spr")
                {
                    issues.Add($"Specification '{spec.Attribut}' has no XPath expression");
                }

                if (string.IsNullOrEmpty(spec.Profile))
                {
                    issues.Add($"Specification '{spec.Attribut}' has no FHIR profile");
                }
            }

            // Check for duplicate IDs
            var duplicateIds = specifications
                .GroupBy(s => s.ID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateId in duplicateIds)
            {
                issues.Add($"Duplicate specification ID found: {duplicateId}");
            }

            // Check for duplicate attributes
            var duplicateAttributes = specifications
                .Where(s => !string.IsNullOrEmpty(s.Attribut))
                .GroupBy(s => s.Attribut)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateAttr in duplicateAttributes)
            {
                issues.Add($"Duplicate specification attribute found: {duplicateAttr}");
            }

            return issues;
        }
    }
}
