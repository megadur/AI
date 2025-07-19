using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ERezeptAbgabeExtractor.Models;

namespace ERezeptAbgabeExtractor.Serialization
{
    /// <summary>
    /// Helper class for serializing eRezept data to various formats
    /// </summary>
    public static class ERezeptSerializer
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Converters = { new StringEnumConverter() }
        };

        /// <summary>
        /// Serializes eRezept data to JSON
        /// </summary>
        /// <param name="data">The data to serialize</param>
        /// <returns>JSON string</returns>
        public static string ToJson(ERezeptAbgabeData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return JsonConvert.SerializeObject(data, _jsonSettings);
        }

        /// <summary>
        /// Deserializes eRezept data from JSON
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <returns>ERezeptData object</returns>
        public static ERezeptAbgabeData? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

            return JsonConvert.DeserializeObject<ERezeptAbgabeData>(json, _jsonSettings);
        }

        /// <summary>
        /// Saves eRezept data to a JSON file
        /// </summary>
        /// <param name="data">The data to save</param>
        /// <param name="filePath">The file path</param>
        public static void SaveToJsonFile(ERezeptAbgabeData data, string filePath)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var json = ToJson(data);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads eRezept data from a JSON file
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>ERezeptData object</returns>
        public static ERezeptAbgabeData? LoadFromJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        /// <summary>
        /// Creates a summary report of the eRezept data
        /// </summary>
        /// <param name="data">The data to summarize</param>
        /// <returns>Summary string</returns>
        public static string CreateSummaryReport(ERezeptAbgabeData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== eRezept Data Summary ===");
            report.AppendLine($"Bundle ID: {data.BundleId}");
            report.AppendLine($"Prescription ID: {data.PrescriptionId}");
            report.AppendLine($"Timestamp: {data.Timestamp:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            report.AppendLine("=== Pharmacy Information ===");
            report.AppendLine($"Name: {data.Pharmacy.Name}");
            report.AppendLine($"IK Number: {data.Pharmacy.IK_Number}");
            report.AppendLine($"Address: {data.Pharmacy.Address.FullAddress}");
            report.AppendLine();

            report.AppendLine("=== Medication Dispense ===");
            report.AppendLine($"Status: {data.MedicationDispense.Status}");
            report.AppendLine($"Handed Over: {data.MedicationDispense.HandedOverDate:yyyy-MM-dd}");
            report.AppendLine();

            report.AppendLine("=== Invoice Information ===");
            report.AppendLine($"Status: {data.Invoice.Status}");
            report.AppendLine($"Total Gross: {data.Invoice.TotalGross:C} {data.Invoice.Currency}");
            report.AppendLine($"Copayment: {data.Invoice.Copayment:C} {data.Invoice.Currency}");
            report.AppendLine($"Line Items: {data.Invoice.LineItems.Count}");

            if (data.Invoice.LineItems.Any())
            {
                report.AppendLine();
                report.AppendLine("=== Line Items ===");
                foreach (var item in data.Invoice.LineItems)
                {
                    report.AppendLine($"  {item.Sequence}. PZN: {item.PZN}, Amount: {item.Amount:C} {item.Currency}");
                    if (item.CopaymentAmount > 0)
                    {
                        report.AppendLine($"     Copayment: {item.CopaymentAmount:C}, Category: {item.CopaymentCategory}");
                    }
                }
            }

            return report.ToString();
        }
    }
}
