using ERezeptAbgabeExtractor.Models;

namespace ERezeptAbgabeExtractor.Validation
{
    /// <summary>
    /// Validation rules specific to KBV practitioner data
    /// </summary>
    public static class KBVPractitionerValidator
    {
        /// <summary>
        /// Validates KBV practitioner data according to specification requirements
        /// </summary>
        /// <param name="data">Extended eRezept data with practitioner information</param>
        /// <returns>List of validation errors</returns>
        public static List<string> ValidateKBVData(ExtendedERezeptData data)
        {
            var errors = new List<string>();
            
            // Validate base eRezept data first
            var baseErrors = ERezeptValidator.Validate(data);
            errors.AddRange(baseErrors);
            
            // Validate practitioner-specific data
            ValidatePractitioner(data.Practitioner, errors);
            
            return errors;
        }

        private static void ValidatePractitioner(PractitionerInfo practitioner, List<string> errors)
        {
            if (practitioner == null)
            {
                errors.Add("Practitioner information is missing");
                return;
            }

            // Validate LANR (ID 42)
            ValidateLANR(practitioner.LANR, "LANR", errors);
            
            // Validate LANR_Responsible (ID 52) - can be empty
            if (!string.IsNullOrEmpty(practitioner.LANR_Responsible))
            {
                ValidateLANR(practitioner.LANR_Responsible, "LANR_Responsible", errors);
            }
            
            // Validate that if LANR_Responsible is filled, there should be assistant qualification
            ValidateLANRLogic(practitioner, errors);
            
            // Validate name information
            ValidateName(practitioner.Name, errors);
            
            // Validate qualifications
            ValidateQualifications(practitioner.Qualifications, errors);
        }

        private static void ValidateLANR(string lanr, string fieldName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(lanr))
            {
                errors.Add($"{fieldName} is missing");
                return;
            }

            // LANR must be exactly 9 characters
            if (lanr.Length != 9)
            {
                errors.Add($"{fieldName} must be exactly 9 characters long (current: {lanr.Length})");
            }

            // LANR must be alphanumeric
            if (!lanr.All(c => char.IsLetterOrDigit(c)))
            {
                errors.Add($"{fieldName} must be alphanumeric only");
            }

            // Check for default value
            if (lanr == "000000000")
            {
                // This is acceptable according to specification when no LANR is available
                // Could add a warning instead of error if needed
            }
        }

        private static void ValidateLANRLogic(PractitionerInfo practitioner, List<string> errors)
        {
            var hasAssistant = practitioner.Qualifications.Any(q => q.TypeCode == "03");
            var hasResponsible = practitioner.Qualifications.Any(q => q.TypeCode == "00" || q.TypeCode == "04");
            
            if (!string.IsNullOrEmpty(practitioner.LANR_Responsible))
            {
                if (!hasAssistant)
                {
                    errors.Add("LANR_Responsible should only be filled when an assistant (qualification type 03) is present");
                }
                
                if (!hasResponsible)
                {
                    errors.Add("LANR_Responsible is filled but no responsible doctor qualification (type 00 or 04) found");
                }
            }
            
            if (hasAssistant && string.IsNullOrEmpty(practitioner.LANR_Responsible))
            {
                errors.Add("When assistant qualification is present, LANR_Responsible should be filled with responsible doctor's LANR");
            }
        }

        private static void ValidateName(PractitionerNameInfo name, List<string> errors)
        {
            if (name == null)
            {
                errors.Add("Practitioner name information is missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(name.Family) && string.IsNullOrWhiteSpace(name.Given))
            {
                errors.Add("Practitioner must have at least family name or given name");
            }
        }

        private static void ValidateQualifications(List<QualificationInfo> qualifications, List<string> errors)
        {
            if (qualifications == null || !qualifications.Any())
            {
                errors.Add("Practitioner must have at least one qualification");
                return;
            }

            foreach (var qual in qualifications)
            {
                if (string.IsNullOrWhiteSpace(qual.TypeCode))
                {
                    errors.Add("Qualification type code is missing");
                }
                else
                {
                    // Validate known qualification types
                    if (!IsValidQualificationType(qual.TypeCode))
                    {
                        errors.Add($"Unknown qualification type code: {qual.TypeCode}");
                    }
                }
            }

            // Check for conflicting qualifications
            var assistantCount = qualifications.Count(q => q.TypeCode == "03");
            var responsibleCount = qualifications.Count(q => q.TypeCode == "00" || q.TypeCode == "04");

            if (assistantCount > 1)
            {
                errors.Add("Multiple assistant qualifications found - only one expected");
            }

            if (responsibleCount > 1)
            {
                errors.Add("Multiple responsible doctor qualifications found - only one expected");
            }
        }

        private static bool IsValidQualificationType(string typeCode)
        {
            return typeCode switch
            {
                "00" => true, // Arzt (Doctor)
                "03" => true, // Assistenz (Assistant)  
                "04" => true, // Verantwortlicher Arzt (Responsible Doctor)
                _ => false
            };
        }

        /// <summary>
        /// Gets the description for a qualification type code
        /// </summary>
        /// <param name="typeCode">The qualification type code</param>
        /// <returns>Human-readable description</returns>
        public static string GetQualificationTypeDescription(string typeCode)
        {
            return typeCode switch
            {
                "00" => "Arzt (Doctor)",
                "03" => "Assistenz (Assistant)",
                "04" => "Verantwortlicher Arzt (Responsible Doctor)",
                _ => $"Unknown type: {typeCode}"
            };
        }
    }
}
