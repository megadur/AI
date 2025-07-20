using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERezeptExtractor.Models
{
    /// <summary>
    /// Represents a single specification rule from the AVD CSV specification
    /// </summary>
    public class AVDSpecification
    {
        /// <summary>
        /// Attribute name (e.g., "la_nr", "asv_nr")
        /// </summary>
        public string Attribut { get; set; } = string.Empty;

        /// <summary>
        /// Unique ID for the attribute
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// German description of the attribute
        /// </summary>
        public string BeschreibungDesAttributs { get; set; } = string.Empty;

        /// <summary>
        /// Length specification (e.g., "9", "leer oder 9", "..12")
        /// </summary>
        public string Laenge { get; set; } = string.Empty;

        /// <summary>
        /// Data type/format (alphanumerisch, numerisch)
        /// </summary>
        public string Darstellung { get; set; } = string.Empty;

        /// <summary>
        /// FHIR profile URL(s)
        /// </summary>
        public string Profile { get; set; } = string.Empty;

        /// <summary>
        /// Additional adaptation requirements marker
        /// </summary>
        public string AnpassungNotwendigZusaetzl { get; set; } = string.Empty;

        /// <summary>
        /// XPath expression(s) for data extraction
        /// </summary>
        public string PartiellerXPath { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this field requires special processing
        /// </summary>
        public bool RequiresAdaptation => !string.IsNullOrEmpty(AnpassungNotwendigZusaetzl);

        /// <summary>
        /// Indicates if this field is optional (can be empty)
        /// </summary>
        public bool IsOptional => Laenge.Contains("leer");

        /// <summary>
        /// Extracted business logic rules from the XPath comments
        /// </summary>
        public List<string> BusinessRules { get; set; } = new List<string>();

        /// <summary>
        /// Parsed XPath expressions
        /// </summary>
        public List<string> XPathExpressions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Container for extracted data based on AVD specifications
    /// </summary>
    public class AVDExtractedData
    {
        public Dictionary<string, object?> ExtractedValues { get; set; } = new Dictionary<string, object?>();
        
        public List<string> Errors { get; set; } = new List<string>();
        
        public List<string> Warnings { get; set; } = new List<string>();
        
        public DateTime ExtractionTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a strongly typed value for a specific attribute
        /// </summary>
        public T? GetValue<T>(string attribut)
        {
            if (ExtractedValues.TryGetValue(attribut, out var value) && value != null)
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception)
                {
                    return default(T);
                }
            }
            return default(T);
        }

        /// <summary>
        /// Sets a value for a specific attribute
        /// </summary>
        public void SetValue(string attribut, object? value)
        {
            ExtractedValues[attribut] = value;
        }

        /// <summary>
        /// Validates extracted data against specifications
        /// </summary>
        public ValidationResult ValidateData(List<AVDSpecification> specifications)
        {
            var results = new List<ValidationResult>();
            
            foreach (var spec in specifications)
            {
                var value = GetValue<string>(spec.Attribut);
                
                // Check required fields
                if (!spec.IsOptional && string.IsNullOrEmpty(value))
                {
                    results.Add(new ValidationResult(
                        $"Required field '{spec.Attribut}' (ID: {spec.ID}) is missing",
                        new[] { spec.Attribut }));
                }

                // Check length constraints
                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(spec.Laenge))
                {
                    var lengthValidation = ValidateLength(value, spec.Laenge, spec.Attribut);
                    if (lengthValidation != ValidationResult.Success)
                    {
                        results.Add(lengthValidation);
                    }
                }

                // Check data type constraints
                if (!string.IsNullOrEmpty(value))
                {
                    var typeValidation = ValidateDataType(value, spec.Darstellung, spec.Attribut);
                    if (typeValidation != ValidationResult.Success)
                    {
                        results.Add(typeValidation);
                    }
                }
            }

            return results.Count == 0 ? ValidationResult.Success! : 
                new ValidationResult(string.Join("; ", results.Select(r => r.ErrorMessage)));
        }

        private ValidationResult ValidateLength(string value, string lengthSpec, string attribut)
        {
            // Parse length specifications like "9", "..12", "leer oder 9", "1..n"
            if (lengthSpec.Contains(".."))
            {
                var parts = lengthSpec.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out int maxLength))
                {
                    if (value.Length > maxLength)
                    {
                        return new ValidationResult(
                            $"Field '{attribut}' exceeds maximum length of {maxLength}",
                            new[] { attribut });
                    }
                }
            }
            else if (int.TryParse(lengthSpec, out int exactLength))
            {
                if (value.Length != exactLength)
                {
                    return new ValidationResult(
                        $"Field '{attribut}' must be exactly {exactLength} characters",
                        new[] { attribut });
                }
            }

            return ValidationResult.Success!;
        }

        private ValidationResult ValidateDataType(string value, string dataType, string attribut)
        {
            switch (dataType?.ToLower())
            {
                case "numerisch":
                    if (!value.All(char.IsDigit))
                    {
                        return new ValidationResult(
                            $"Field '{attribut}' must contain only numeric characters",
                            new[] { attribut });
                    }
                    break;
                case "alphanumerisch":
                    // Generally allows letters, numbers, and some special characters
                    // No strict validation needed for alphanumeric in this context
                    break;
            }

            return ValidationResult.Success!;
        }
    }
}
