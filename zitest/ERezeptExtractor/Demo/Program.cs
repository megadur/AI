using ERezeptExtractor;
using ERezeptExtractor.KBV;
using ERezeptExtractor.Validation;
using ERezeptExtractor.Serialization;

namespace ERezeptExtractor.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== eRezept Data Extractor Demo with KBV Support ===");
                Console.WriteLine();

                // Path to the example XML file
                var xmlFilePath = @"d:\data\code\_examples\AI\zitest\eRezeptAbgabedaten.xml";
                
                if (!File.Exists(xmlFilePath))
                {
                    Console.WriteLine($"Error: XML file not found at {xmlFilePath}");
                    Console.WriteLine("Please ensure the eRezeptAbgabedaten.xml file exists in the correct location.");
                    return;
                }

                Console.WriteLine($"Processing file: {xmlFilePath}");
                Console.WriteLine();

                // ===== Basic eRezept Extraction =====
                Console.WriteLine("=== Basic eRezept Extraction ===");
                var extractor = new ERezeptExtractor.ERezeptExtractor();
                var extractedData = extractor.ExtractFromFile(xmlFilePath);

                // Basic validation
                var validationErrors = ERezeptValidator.Validate(extractedData);
                
                if (validationErrors.Any())
                {
                    Console.WriteLine("Basic Validation Errors:");
                    foreach (var error in validationErrors)
                    {
                        Console.WriteLine($"- {error}");
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("✓ Basic data validation passed");
                    Console.WriteLine();
                }

                // ===== KBV Practitioner Extraction =====
                Console.WriteLine("=== KBV Practitioner Extraction ===");
                var kbvExtractor = new KBVPractitionerExtractor();
                var kbvData = kbvExtractor.ExtractKBVData(xmlFilePath);

                // KBV validation
                var kbvValidationErrors = KBVPractitionerValidator.ValidateKBVData(kbvData);
                
                if (kbvValidationErrors.Any())
                {
                    Console.WriteLine("KBV Validation Errors:");
                    foreach (var error in kbvValidationErrors)
                    {
                        Console.WriteLine($"- {error}");
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("✓ KBV data validation passed");
                    Console.WriteLine();
                }

                // ===== Display KBV Practitioner Information =====
                if (kbvData.Practitioner != null)
                {
                    Console.WriteLine("=== KBV Practitioner Information ===");
                    Console.WriteLine($"Name: {kbvData.Practitioner.Name.Given} {kbvData.Practitioner.Name.Family}");
                    Console.WriteLine($"LANR (Prescribing): {kbvData.Practitioner.LANR}");
                    Console.WriteLine($"LANR (Responsible): {kbvData.Practitioner.LANR_Responsible ?? "Not specified"}");
                    Console.WriteLine($"Organization: {kbvData.Practitioner.Organization ?? "Not specified"}");
                    
                    if (kbvData.Practitioner.Qualifications.Any())
                    {
                        Console.WriteLine("Qualifications:");
                        foreach (var qual in kbvData.Practitioner.Qualifications)
                        {
                            var description = KBVPractitionerValidator.GetQualificationTypeDescription(qual.TypeCode);
                            Console.WriteLine($"  - Type: {qual.TypeCode} ({description})");
                            Console.WriteLine($"    Text: {qual.Text ?? "Not specified"}");
                        }
                    }
                    Console.WriteLine();
                }

                // ===== Display Summary Report =====
                var summaryReport = ERezeptSerializer.CreateSummaryReport(extractedData);
                Console.WriteLine(summaryReport);

                // ===== Save Data =====
                // Save basic data
                var jsonOutputPath = Path.Combine(Path.GetDirectoryName(xmlFilePath)!, "extracted_data.json");
                ERezeptSerializer.SaveToJsonFile(extractedData, jsonOutputPath);
                Console.WriteLine($"✓ Basic data saved to JSON file: {jsonOutputPath}");

                // Save KBV extended data
                var kbvJsonOutputPath = Path.Combine(Path.GetDirectoryName(xmlFilePath)!, "extracted_data_kbv.json");
                ERezeptSerializer.SaveToJsonFile(kbvData, kbvJsonOutputPath);
                Console.WriteLine($"✓ KBV extended data saved to JSON file: {kbvJsonOutputPath}");
                Console.WriteLine();

                // ===== Additional Validation Checks =====
                Console.WriteLine("=== Additional Validation Checks ===");
                
                if (extractedData.Invoice.LineItems.Any())
                {
                    var firstLineItem = extractedData.Invoice.LineItems.First();
                    Console.WriteLine($"PZN Validation: {(ERezeptValidator.IsValidPZN(firstLineItem.PZN) ? "✓ Valid" : "✗ Invalid")} ({firstLineItem.PZN})");
                }

                Console.WriteLine($"IK Number Validation: {(ERezeptValidator.IsValidIKNumber(extractedData.Pharmacy.IK_Number) ? "✓ Valid" : "✗ Invalid")} ({extractedData.Pharmacy.IK_Number})");

                // KBV specific validations
                if (kbvData.Practitioner != null)
                {
                    Console.WriteLine($"LANR Validation: {(kbvData.Practitioner.LANR.Length == 9 ? "✓ Valid format" : "✗ Invalid format")} ({kbvData.Practitioner.LANR})");
                    
                    var hasAssistant = kbvData.Practitioner.Qualifications.Any(q => q.TypeCode == "03");
                    var hasResponsible = kbvData.Practitioner.Qualifications.Any(q => q.TypeCode == "00" || q.TypeCode == "04");
                    Console.WriteLine($"Qualification Logic: {(hasAssistant && !string.IsNullOrEmpty(kbvData.Practitioner.LANR_Responsible) ? "✓ Assistant with responsible doctor" : hasResponsible ? "✓ Responsible doctor only" : "? No clear qualification pattern")}");
                }

                Console.WriteLine();
                Console.WriteLine("=== Processing Complete ===");
                Console.WriteLine("Two files have been generated:");
                Console.WriteLine($"1. Basic eRezept data: {jsonOutputPath}");
                Console.WriteLine($"2. KBV extended data: {kbvJsonOutputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
