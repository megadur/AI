using ERezeptExtractor.TA7;
using System.Text;

namespace ERezeptExtractor.Demo
{
    /// <summary>
    /// Demo application for TA7 FHIR report generation
    /// Demonstrates how to create German electronic prescription billing reports
    /// </summary>
    class TA7Demo
    {
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine("=== TA7 FHIR Report Generator Demo ===");
                Console.WriteLine("German Electronic Prescription Billing (GKVSV Specification)");
                Console.WriteLine();

                // Create generator and validator
                var generator = new TA7FhirReportGenerator();
                var validator = new TA7ReportValidator();

                // Demo 1: Generate sample report
                Console.WriteLine("1. Generating Sample TA7 Report...");
                var sampleReport = generator.CreateSampleReport();
                
                // Validate the report model
                Console.WriteLine("2. Validating Report Model...");
                var modelErrors = validator.ValidateReport(sampleReport);
                Console.WriteLine(validator.GetValidationSummary(modelErrors));

                // Generate XML
                Console.WriteLine("3. Generating XML...");
                var xmlContent = generator.GenerateTA7Report(sampleReport);
                
                // Validate the generated XML
                Console.WriteLine("4. Validating Generated XML...");
                var xmlErrors = validator.ValidateXml(xmlContent);
                Console.WriteLine(validator.GetValidationSummary(xmlErrors));

                // Save the generated report
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "ta7_sample_report.xml");
                File.WriteAllText(outputPath, xmlContent, Encoding.UTF8);
                Console.WriteLine($"✓ Sample report saved to: {outputPath}");
                Console.WriteLine();

                // Demo 2: Create custom report
                Console.WriteLine("5. Creating Custom TA7 Report...");
                var customReport = CreateCustomReport();
                
                var customModelErrors = validator.ValidateReport(customReport);
                Console.WriteLine("Custom Report Model Validation:");
                Console.WriteLine(validator.GetValidationSummary(customModelErrors));

                var customXml = generator.GenerateTA7Report(customReport);
                var customXmlErrors = validator.ValidateXml(customXml);
                Console.WriteLine("Custom Report XML Validation:");
                Console.WriteLine(validator.GetValidationSummary(customXmlErrors));

                var customOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "ta7_custom_report.xml");
                File.WriteAllText(customOutputPath, customXml, Encoding.UTF8);
                Console.WriteLine($"✓ Custom report saved to: {customOutputPath}");
                Console.WriteLine();

                // Demo 3: Show report structure
                Console.WriteLine("6. TA7 Report Structure Overview:");
                ShowReportStructure(sampleReport);

                // Demo 4: Performance test
                Console.WriteLine("\n7. Performance Test (Generating 100 reports)...");
                PerformanceTest(generator, validator);

                Console.WriteLine("\n=== Demo Complete ===");
                Console.WriteLine("Key Features Demonstrated:");
                Console.WriteLine("- ✓ GKVSV-compliant TA7 FHIR report generation");
                Console.WriteLine("- ✓ Comprehensive validation (model + XML)");
                Console.WriteLine("- ✓ German health insurance billing standards");
                Console.WriteLine("- ✓ Structured data models for easy integration");
                Console.WriteLine("- ✓ Error handling and validation reporting");
                Console.WriteLine("- ✓ Sample and custom report generation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Create a custom TA7 report with specific data
        /// </summary>
        static TA7ReportModels.TA7Report CreateCustomReport()
        {
            var prescriptionId = "160.123.456.789.012.34";
            var invoiceNumber = "123456789-24-1215";
            var listId = Guid.NewGuid().ToString();
            var bundleId = Guid.NewGuid().ToString();
            var invoiceId = Guid.NewGuid().ToString();

            return new TA7ReportModels.TA7Report
            {
                BundleId = Guid.NewGuid().ToString(),
                ProfileVersion = "1.3",
                Timestamp = DateTime.Now,
                Identifier = new TA7ReportModels.TA7Identifier
                {
                    Dateinummer = "12345",
                    Value = "CUSTOM24001",
                    SystemValue = "https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Dateiname"
                },
                Composition = new TA7ReportModels.TA7Composition
                {
                    Id = Guid.NewGuid().ToString(),
                    IKEmpfaenger = "109876543",
                    IKKostentraeger = "123456789",
                    Rechnungsnummer = invoiceNumber,
                    InvoiceDate = DateTime.Today,
                    Rechnungsdatum = DateTime.Today.AddDays(1),
                    AuthorIK = "987654321",
                    Sections = new List<TA7ReportModels.TA7Section>
                    {
                        new() { Code = "LR", EntryReference = $"urn:uuid:{listId}" },
                        new() { Code = "RB", EntryReference = $"urn:uuid:{bundleId}" }
                    }
                },
                Entries = new List<TA7ReportModels.TA7Entry>
                {
                    // List entry
                    new()
                    {
                        FullUrl = $"urn:uuid:{listId}",
                        Resource = new TA7ReportModels.TA7Resource
                        {
                            Type = "List",
                            Id = listId,
                            Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_TA7_Rechnung_List|1.3",
                            Properties = new Dictionary<string, object>
                            {
                                ["fileName"] = "CUSTOM24001"
                            }
                        }
                    },
                    // Bundle entry
                    new()
                    {
                        FullUrl = $"urn:uuid:{bundleId}",
                        Resource = new TA7ReportModels.TA7Resource
                        {
                            Type = "Bundle",
                            Id = bundleId,
                            Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_TA7_RezeptBundle|1.3",
                            Properties = new Dictionary<string, object>
                            {
                                ["entries"] = new List<TA7ReportModels.TA7RezeptEntry>
                                {
                                    new()
                                    {
                                        Url = "https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Bundle",
                                        FullUrl = $"urn:uuid:{Guid.NewGuid()}",
                                        Resource = new TA7ReportModels.TA7Binary
                                        {
                                            ContentType = "application/pkcs7-mime",
                                            Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("<Custom KBV Bundle>"))
                                        }
                                    }
                                }
                            }
                        }
                    },
                    // Invoice entry with detailed line items
                    new()
                    {
                        FullUrl = $"urn:uuid:{invoiceId}",
                        Resource = new TA7ReportModels.TA7Resource
                        {
                            Type = "Invoice",
                            Id = invoiceId,
                            Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_ERP_eAbrechnungsdaten|1.3",
                            Properties = new Dictionary<string, object>
                            {
                                ["invoice"] = new TA7ReportModels.TA7Invoice
                                {
                                    Id = invoiceId,
                                    PrescriptionId = prescriptionId,
                                    Belegnummer = "2024121500012345678",
                                    Status = "issued",
                                    Issuer = new TA7ReportModels.TA7Issuer
                                    {
                                        Sitz = "1",
                                        LeistungserbringerTyp = "A",
                                        IKNumber = "300123456"
                                    },
                                    LineItems = new List<TA7ReportModels.TA7LineItem>
                                    {
                                        new()
                                        {
                                            Sequence = 1,
                                            Positionstyp = "1",
                                            Import = "0",
                                            VatValue = 3.15m,
                                            ChargeItemCode = "UNC",
                                            PriceComponents = new List<TA7ReportModels.TA7PriceComponent>
                                            {
                                                new() { Type = "deduction", Code = "R001", Amount = -5.00m, Currency = "EUR" },
                                                new() { Type = "deduction", Code = "R004", Amount = 0.00m, Currency = "EUR" },
                                                new() { Type = "deduction", Code = "R005", Amount = -1.50m, Currency = "EUR" }
                                            }
                                        },
                                        new()
                                        {
                                            Sequence = 2,
                                            Positionstyp = "1",
                                            Import = "0",
                                            VatValue = 1.90m,
                                            ChargeItemCode = "UNC",
                                            PriceComponents = new List<TA7ReportModels.TA7PriceComponent>
                                            {
                                                new() { Type = "deduction", Code = "R001", Amount = -2.50m, Currency = "EUR" },
                                                new() { Type = "deduction", Code = "R006", Amount = 0.00m, Currency = "EUR" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Display report structure overview
        /// </summary>
        static void ShowReportStructure(TA7ReportModels.TA7Report report)
        {
            Console.WriteLine("TA7 Report Structure:");
            Console.WriteLine($"├── Bundle ID: {report.BundleId}");
            Console.WriteLine($"├── Profile Version: {report.ProfileVersion}");
            Console.WriteLine($"├── Timestamp: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"├── Identifier: {report.Identifier.Value} (Dateinummer: {report.Identifier.Dateinummer})");
            Console.WriteLine("├── Composition:");
            Console.WriteLine($"│   ├── IK Empfänger: {report.Composition.IKEmpfaenger}");
            Console.WriteLine($"│   ├── IK Kostenträger: {report.Composition.IKKostentraeger}");
            Console.WriteLine($"│   ├── Rechnungsnummer: {report.Composition.Rechnungsnummer}");
            Console.WriteLine($"│   ├── Invoice Date: {report.Composition.InvoiceDate:yyyy-MM-dd}");
            Console.WriteLine($"│   ├── Rechnungsdatum: {report.Composition.Rechnungsdatum:yyyy-MM-dd}");
            Console.WriteLine($"│   ├── Author IK: {report.Composition.AuthorIK}");
            Console.WriteLine($"│   └── Sections: {string.Join(", ", report.Composition.Sections.Select(s => s.Code))}");
            Console.WriteLine("└── Entries:");

            for (int i = 0; i < report.Entries.Count; i++)
            {
                var entry = report.Entries[i];
                var isLast = i == report.Entries.Count - 1;
                var prefix = isLast ? "    └──" : "    ├──";
                
                Console.WriteLine($"{prefix} {entry.Resource.Type} ({entry.Resource.Id})");
                
                if (entry.Resource.Type == "Invoice" && 
                    entry.Resource.Properties.TryGetValue("invoice", out var invoiceObj) &&
                    invoiceObj is TA7ReportModels.TA7Invoice invoice)
                {
                    var linePrefix = isLast ? "        " : "    │   ";
                    Console.WriteLine($"{linePrefix}├── Prescription ID: {invoice.PrescriptionId}");
                    Console.WriteLine($"{linePrefix}├── Belegnummer: {invoice.Belegnummer}");
                    Console.WriteLine($"{linePrefix}├── Issuer IK: {invoice.Issuer.IKNumber}");
                    Console.WriteLine($"{linePrefix}└── Line Items: {invoice.LineItems.Count}");
                }
            }
        }

        /// <summary>
        /// Performance test for report generation
        /// </summary>
        static void PerformanceTest(TA7FhirReportGenerator generator, TA7ReportValidator validator)
        {
            const int reportCount = 100;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var generationTimes = new List<long>();
            var validationTimes = new List<long>();
            var totalErrors = 0;

            for (int i = 0; i < reportCount; i++)
            {
                // Generate report
                var genStart = stopwatch.ElapsedMilliseconds;
                var report = generator.CreateSampleReport();
                // Modify some data to make each report unique
                report.BundleId = Guid.NewGuid().ToString();
                report.Identifier.Value = $"TEST{i:D5}001";
                report.Composition.Rechnungsnummer = $"12345678{i % 10}-24-{1000 + i:D4}";
                
                var xml = generator.GenerateTA7Report(report);
                var genTime = stopwatch.ElapsedMilliseconds - genStart;
                generationTimes.Add(genTime);

                // Validate report
                var valStart = stopwatch.ElapsedMilliseconds;
                var errors = validator.ValidateReport(report);
                var xmlErrors = validator.ValidateXml(xml);
                var valTime = stopwatch.ElapsedMilliseconds - valStart;
                validationTimes.Add(valTime);
                
                totalErrors += errors.Count + xmlErrors.Count;

                if ((i + 1) % 25 == 0)
                {
                    Console.Write($"\rProgress: {i + 1}/{reportCount} reports processed...");
                }
            }

            stopwatch.Stop();

            Console.WriteLine($"\rPerformance Test Results:");
            Console.WriteLine($"├── Total Reports: {reportCount}");
            Console.WriteLine($"├── Total Time: {stopwatch.ElapsedMilliseconds:N0} ms");
            Console.WriteLine($"├── Average Generation Time: {generationTimes.Average():F2} ms");
            Console.WriteLine($"├── Average Validation Time: {validationTimes.Average():F2} ms");
            Console.WriteLine($"├── Total Validation Errors: {totalErrors}");
            Console.WriteLine($"└── Reports per Second: {(reportCount * 1000.0 / stopwatch.ElapsedMilliseconds):F1}");
        }
    }
}
