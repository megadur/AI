using ERezeptExtractor;
using ERezeptExtractor.Validation;
using ERezeptExtractor.Serialization;

namespace ERezeptExtractor.Examples
{
    /// <summary>
    /// Example usage of the ERezeptExtractor library
    /// </summary>
    public class UsageExamples
    {
        /// <summary>
        /// Basic usage example
        /// </summary>
        public static void BasicUsage()
        {
            // Initialize the extractor
            var extractor = new ERezeptExtractor();

            // Extract from XML file
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");

            // Validate the data
            var errors = ERezeptValidator.Validate(data);
            if (errors.Any())
            {
                Console.WriteLine("Validation errors found:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"- {error}");
                }
            }

            // Access extracted data
            Console.WriteLine($"Prescription ID: {data.PrescriptionId}");
            Console.WriteLine($"Pharmacy: {data.Pharmacy.Name}");
            Console.WriteLine($"Total Amount: {data.Invoice.TotalGross:C} {data.Invoice.Currency}");
        }

        /// <summary>
        /// Extract from XML string
        /// </summary>
        public static void ExtractFromString()
        {
            var xmlContent = File.ReadAllText("path/to/eRezeptAbgabedaten.xml");
            
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromXml(xmlContent);

            Console.WriteLine($"Bundle ID: {data.BundleId}");
        }

        /// <summary>
        /// JSON serialization example
        /// </summary>
        public static void JsonSerialization()
        {
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");

            // Convert to JSON
            var json = ERezeptSerializer.ToJson(data);
            Console.WriteLine("JSON Output:");
            Console.WriteLine(json);

            // Save to file
            ERezeptSerializer.SaveToJsonFile(data, "output.json");

            // Load from file
            var loadedData = ERezeptSerializer.LoadFromJsonFile("output.json");
            Console.WriteLine($"Loaded data - Pharmacy: {loadedData?.Pharmacy.Name}");
        }

        /// <summary>
        /// Generate summary report
        /// </summary>
        public static void SummaryReport()
        {
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");

            var report = ERezeptSerializer.CreateSummaryReport(data);
            Console.WriteLine(report);
        }

        /// <summary>
        /// Access line item details
        /// </summary>
        public static void LineItemDetails()
        {
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");

            foreach (var lineItem in data.Invoice.LineItems)
            {
                Console.WriteLine($"PZN: {lineItem.PZN}");
                Console.WriteLine($"Amount: {lineItem.Amount:C} {lineItem.Currency}");
                Console.WriteLine($"VAT Rate: {lineItem.VatRate}%");
                Console.WriteLine($"Copayment: {lineItem.CopaymentAmount:C}");
                
                // Access Zusatzattribute
                var zusatz = lineItem.Zusatzattribute;
                Console.WriteLine($"Markt - Gruppe: {zusatz.Markt.Gruppe}, Schlüssel: {zusatz.Markt.Schluessel}");
                Console.WriteLine($"Rabattvertrag - Gruppe: {zusatz.Rabattvertragserfuellung.Gruppe}, Schlüssel: {zusatz.Rabattvertragserfuellung.Schluessel}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Validation example
        /// </summary>
        public static void ValidationExample()
        {
            // Validate PZN
            var pzn = "05454378";
            var isPznValid = ERezeptValidator.IsValidPZN(pzn);
            Console.WriteLine($"PZN {pzn} is {(isPznValid ? "valid" : "invalid")}");

            // Validate IK Number
            var ikNumber = "123456789";
            var isIkValid = ERezeptValidator.IsValidIKNumber(ikNumber);
            Console.WriteLine($"IK Number {ikNumber} is {(isIkValid ? "valid" : "invalid")}");

            // Comprehensive validation
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");
            
            var validationErrors = ERezeptValidator.Validate(data);
            if (validationErrors.Any())
            {
                Console.WriteLine("Data validation failed:");
                foreach (var error in validationErrors)
                {
                    Console.WriteLine($"- {error}");
                }
            }
            else
            {
                Console.WriteLine("Data validation passed successfully!");
            }
        }

        /// <summary>
        /// Error handling example
        /// </summary>
        public static void ErrorHandling()
        {
            var extractor = new ERezeptExtractor();

            try
            {
                // This will throw FileNotFoundException
                var data = extractor.ExtractFromFile("nonexistent.xml");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File not found: {ex.Message}");
            }

            try
            {
                // This will throw ArgumentException
                var data = extractor.ExtractFromXml("");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Invalid input: {ex.Message}");
            }

            try
            {
                // This will throw XmlException for invalid XML
                var data = extractor.ExtractFromXml("invalid xml content");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XML parsing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Working with pharmacy address
        /// </summary>
        public static void PharmacyAddressExample()
        {
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");

            var pharmacy = data.Pharmacy;
            Console.WriteLine($"Pharmacy: {pharmacy.Name}");
            Console.WriteLine($"IK Number: {pharmacy.IK_Number}");
            
            var address = pharmacy.Address;
            Console.WriteLine($"Street: {address.Street} {address.HouseNumber}");
            Console.WriteLine($"City: {address.PostalCode} {address.City}");
            Console.WriteLine($"Country: {address.Country}");
            Console.WriteLine($"Full Address: {address.FullAddress}");
        }

        /// <summary>
        /// Working with medication dispense information
        /// </summary>
        public static void MedicationDispenseExample()
        {
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromFile("path/to/eRezeptAbgabedaten.xml");

            var dispense = data.MedicationDispense;
            Console.WriteLine($"Dispense ID: {dispense.Id}");
            Console.WriteLine($"Status: {dispense.Status}");
            Console.WriteLine($"Type: {dispense.Type}");
            Console.WriteLine($"Prescription Reference: {dispense.PrescriptionId}");
            
            if (dispense.HandedOverDate.HasValue)
            {
                Console.WriteLine($"Handed Over: {dispense.HandedOverDate.Value:yyyy-MM-dd}");
            }
        }
    }
}
