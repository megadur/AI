using ERezeptExtractor;
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
                Console.WriteLine("=== eRezept Data Extractor Demo ===");
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

                // Create extractor instance
                var extractor = new ERezeptExtractor();

                // Extract data from XML file
                var extractedData = extractor.ExtractFromFile(xmlFilePath);

                // Validate the extracted data
                var validationErrors = ERezeptValidator.Validate(extractedData);
                
                if (validationErrors.Any())
                {
                    Console.WriteLine("=== Validation Errors ===");
                    foreach (var error in validationErrors)
                    {
                        Console.WriteLine($"- {error}");
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("✓ Data validation passed");
                    Console.WriteLine();
                }

                // Display summary report
                var summaryReport = ERezeptSerializer.CreateSummaryReport(extractedData);
                Console.WriteLine(summaryReport);

                // Save to JSON file
                var jsonOutputPath = Path.Combine(Path.GetDirectoryName(xmlFilePath)!, "extracted_data.json");
                ERezeptSerializer.SaveToJsonFile(extractedData, jsonOutputPath);
                Console.WriteLine($"✓ Data saved to JSON file: {jsonOutputPath}");
                Console.WriteLine();

                // Display JSON output (first 500 characters)
                var jsonOutput = ERezeptSerializer.ToJson(extractedData);
                Console.WriteLine("=== JSON Output (Preview) ===");
                Console.WriteLine(jsonOutput.Length > 500 ? jsonOutput[..500] + "..." : jsonOutput);
                
                // Additional validation checks
                Console.WriteLine();
                Console.WriteLine("=== Additional Validation Checks ===");
                
                if (extractedData.Invoice.LineItems.Any())
                {
                    var firstLineItem = extractedData.Invoice.LineItems.First();
                    Console.WriteLine($"PZN Validation: {(ERezeptValidator.IsValidPZN(firstLineItem.PZN) ? "✓ Valid" : "✗ Invalid")} ({firstLineItem.PZN})");
                }

                Console.WriteLine($"IK Number Validation: {(ERezeptValidator.IsValidIKNumber(extractedData.Pharmacy.IK_Number) ? "✓ Valid" : "✗ Invalid")} ({extractedData.Pharmacy.IK_Number})");

                Console.WriteLine();
                Console.WriteLine("=== Processing Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
