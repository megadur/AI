using ERezeptExtractor.TA7;

namespace ERezeptExtractor.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TA7 FHIR Report Generator Demo ===");
            Console.WriteLine();

            try
            {
                // Create TA7 generator and validator
                var generator = new TA7FhirReportGenerator();
                var validator = new TA7ReportValidator();
                
                Console.WriteLine("1. Creating sample TA7 report...");
                var report = generator.CreateSampleReport();
                Console.WriteLine($"   ✅ Created report with Bundle ID: {report.BundleId}");
                Console.WriteLine($"   ✅ Report identifier: {report.Identifier.Value}");
                Console.WriteLine($"   ✅ Composition sections: {report.Composition.Sections.Count}");
                Console.WriteLine($"   ✅ Report entries: {report.Entries.Count}");
                Console.WriteLine();

                Console.WriteLine("2. Validating the report...");
                var validationErrors = validator.ValidateReport(report);
                if (validationErrors.Any())
                {
                    Console.WriteLine("   ❌ Validation errors found:");
                    foreach (var error in validationErrors)
                    {
                        Console.WriteLine($"      - {error}");
                    }
                }
                else
                {
                    Console.WriteLine("   ✅ Report validation passed!");
                }
                Console.WriteLine();

                Console.WriteLine("3. Generating XML...");
                var xml = generator.GenerateTA7Report(report);
                Console.WriteLine($"   ✅ Generated XML length: {xml.Length} characters");
                Console.WriteLine();

                Console.WriteLine("4. Validating generated XML...");
                var xmlErrors = validator.ValidateXml(xml);
                if (xmlErrors.Any())
                {
                    Console.WriteLine("   ❌ XML validation errors found:");
                    foreach (var error in xmlErrors)
                    {
                        Console.WriteLine($"      - {error}");
                    }
                }
                else
                {
                    Console.WriteLine("   ✅ XML validation passed!");
                }
                Console.WriteLine();

                Console.WriteLine("5. Showing validation summary...");
                var allErrors = validationErrors.Concat(xmlErrors).ToList();
                var summary = validator.GetValidationSummary(allErrors);
                Console.WriteLine(summary);
                Console.WriteLine();

                Console.WriteLine("6. Creating custom report...");
                var customReport = new TA7ReportModels.TA7Report
                {
                    BundleId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now,
                    Identifier = new TA7ReportModels.TA7Identifier
                    {
                        Dateinummer = "99999",
                        Value = "DEMO24001"
                    },
                    Composition = new TA7ReportModels.TA7Composition
                    {
                        Id = "demo-composition",
                        IKEmpfaenger = "123456789",
                        IKKostentraeger = "987654321",
                        Rechnungsnummer = "123456789-24-9999",
                        InvoiceDate = DateTime.Today.AddDays(-1),
                        Rechnungsdatum = DateTime.Today,
                        AuthorIK = "555666777"
                    }
                };

                var customXml = generator.GenerateTA7Report(customReport);
                var customValidationErrors = validator.ValidateReport(customReport);
                
                Console.WriteLine($"   ✅ Custom report created with Bundle ID: {customReport.BundleId}");
                Console.WriteLine($"   ✅ Custom report validation: {(customValidationErrors.Any() ? "Failed" : "Passed")}");
                Console.WriteLine();

                Console.WriteLine("=== TA7 Demo Completed Successfully! ===");
                Console.WriteLine();
                Console.WriteLine("Key Features Demonstrated:");
                Console.WriteLine("• TA7 FHIR report generation");
                Console.WriteLine("• GKVSV compliance validation");
                Console.WriteLine("• XML structure verification");
                Console.WriteLine("• German healthcare identifier validation");
                Console.WriteLine("• Custom report creation");
                Console.WriteLine("• Business rule validation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
