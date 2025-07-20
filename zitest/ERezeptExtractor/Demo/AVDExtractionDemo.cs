using System;
using System.IO;
using System.Threading.Tasks;
using ERezeptExtractor;
using ERezeptExtractor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ERezeptExtractor.Demo
{
    /// <summary>
    /// Demo program showing AVD specification-based FHIR extraction
    /// </summary>
    class AVDExtractionDemo
    {
        private static async Task Main(string[] args)
        {
            // Setup logging and dependency injection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    services.AddScoped<AVDBasedExtractor>();
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<AVDExtractionDemo>>();
            var extractor = host.Services.GetRequiredService<AVDBasedExtractor>();

            try
            {
                logger.LogInformation("=== AVD Specification-Based FHIR Extraction Demo ===");

                // Define file paths
                var specificationPath = Path.Combine(GetProjectRoot(), "doku", "AVD_Daten - Tabellenblatt2.csv");
                var sampleXmlPath = Path.Combine(GetProjectRoot(), "sample_ta7_report.xml");

                // Check if files exist
                if (!File.Exists(specificationPath))
                {
                    logger.LogError("AVD specification file not found: {Path}", specificationPath);
                    Console.WriteLine("Please ensure the AVD specification CSV file is available.");
                    return;
                }

                // Demo 1: Validate specifications
                await DemoSpecificationValidation(extractor, specificationPath, logger);

                // Demo 2: Analyze specification complexity
                await DemoSpecificationAnalysis(extractor, specificationPath, logger);

                // Demo 3: Extract from sample XML (if available)
                if (File.Exists(sampleXmlPath))
                {
                    await DemoFhirExtraction(extractor, sampleXmlPath, specificationPath, logger);
                }
                else
                {
                    await DemoWithSampleXml(extractor, specificationPath, logger);
                }

                Console.WriteLine("\nDemo completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo execution failed");
                Console.WriteLine($"Demo failed: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task DemoSpecificationValidation(AVDBasedExtractor extractor, 
            string specPath, ILogger logger)
        {
            logger.LogInformation("\n=== Demo 1: Specification Validation ===");

            var report = extractor.ValidateSpecifications(specPath);
            
            Console.WriteLine($"Validation Report:");
            Console.WriteLine($"- Total Specifications: {report.TotalSpecifications}");
            Console.WriteLine($"- Complex Specifications: {report.ComplexSpecifications}");
            Console.WriteLine($"- Profile Groups: {report.ProfileGroups.Count}");
            Console.WriteLine($"- Validation Success: {report.Success}");

            if (report.ValidationIssues.Count > 0)
            {
                Console.WriteLine($"\nValidation Issues:");
                foreach (var issue in report.ValidationIssues.Take(5)) // Show first 5
                {
                    Console.WriteLine($"  - {issue}");
                }
                if (report.ValidationIssues.Count > 5)
                {
                    Console.WriteLine($"  ... and {report.ValidationIssues.Count - 5} more");
                }
            }

            Console.WriteLine($"\nProfile Groups Found:");
            foreach (var profile in report.ProfileGroups.Take(5)) // Show first 5
            {
                Console.WriteLine($"  - {profile}");
            }

            await Task.Delay(1000); // Simulate async operation
        }

        private static async Task DemoSpecificationAnalysis(AVDBasedExtractor extractor, 
            string specPath, ILogger logger)
        {
            logger.LogInformation("\n=== Demo 2: Specification Analysis ===");

            var complexSpecs = extractor.GetComplexSpecifications(specPath);
            var profileGroups = extractor.GetSpecificationsByProfile(specPath);

            Console.WriteLine($"Complex Specifications Analysis:");
            Console.WriteLine($"Found {complexSpecs.Count} specifications requiring special business logic:\n");

            foreach (var spec in complexSpecs.Take(10)) // Show first 10
            {
                Console.WriteLine($"Attribute: {spec.Attribut} (ID: {spec.ID})");
                Console.WriteLine($"  Description: {spec.BeschreibungDesAttributs}");
                Console.WriteLine($"  Business Rules: {spec.BusinessRules.Count}");
                Console.WriteLine($"  XPath Expressions: {spec.XPathExpressions.Count}");
                
                if (spec.BusinessRules.Count > 0)
                {
                    Console.WriteLine($"  First Rule: {spec.BusinessRules[0]}");
                }
                Console.WriteLine();
            }

            Console.WriteLine($"\nProfile Distribution:");
            foreach (var group in profileGroups.Take(5))
            {
                var profileName = group.Key.Split('/').LastOrDefault() ?? group.Key;
                Console.WriteLine($"  {profileName}: {group.Value.Count} specifications");
            }

            await Task.Delay(1000);
        }

        private static async Task DemoFhirExtraction(AVDBasedExtractor extractor, string xmlPath, 
            string specPath, ILogger logger)
        {
            logger.LogInformation("\n=== Demo 3: FHIR Extraction ===");

            var result = extractor.ExtractFromFile(xmlPath, specPath);

            Console.WriteLine($"Extraction Results:");
            Console.WriteLine(result.GetSummary());
            
            Console.WriteLine($"\nSample Extracted Values:");
            var sampleCount = 0;
            foreach (var kvp in result.ExtractedData.ExtractedValues)
            {
                if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Value.ToString()) && sampleCount < 10)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    sampleCount++;
                }
            }

            if (result.ExtractedData.Errors.Count > 0)
            {
                Console.WriteLine($"\nExtraction Errors ({result.ExtractedData.Errors.Count}):");
                foreach (var error in result.ExtractedData.Errors.Take(5))
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            if (result.ExtractedData.Warnings.Count > 0)
            {
                Console.WriteLine($"\nExtraction Warnings ({result.ExtractedData.Warnings.Count}):");
                foreach (var warning in result.ExtractedData.Warnings.Take(5))
                {
                    Console.WriteLine($"  - {warning}");
                }
            }

            await Task.Delay(1000);
        }

        private static async Task DemoWithSampleXml(AVDBasedExtractor extractor, string specPath, ILogger logger)
        {
            logger.LogInformation("\n=== Demo 3: FHIR Extraction with Sample XML ===");

            // Create a minimal sample FHIR XML for demonstration
            var sampleXml = CreateSampleFhirXml();
            
            Console.WriteLine("Using sample FHIR XML for demonstration...");

            var result = extractor.ExtractFromXml(sampleXml, specPath);

            Console.WriteLine($"Extraction Results:");
            Console.WriteLine(result.GetSummary());

            Console.WriteLine($"\nNote: This is a minimal sample. Real FHIR documents would contain more data.");
            Console.WriteLine("Extracted values will be mostly empty due to the simplified XML structure.");

            if (result.ExtractedData.Errors.Count > 0)
            {
                Console.WriteLine($"\nSample Errors (showing field mapping attempts):");
                foreach (var error in result.ExtractedData.Errors.Take(3))
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            await Task.Delay(1000);
        }

        private static string CreateSampleFhirXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Bundle xmlns=""http://hl7.org/fhir"">
    <id value=""sample-bundle""/>
    <identifier>
        <system value=""https://gematik.de/fhir/erp/NamingSystem/GEM_ERP_NS_PrescriptionId""/>
        <value value=""160.123.456.789.123.58""/>
    </identifier>
    <entry>
        <resource>
            <Practitioner>
                <id value=""practitioner-1""/>
                <identifier>
                    <system value=""https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR""/>
                    <value value=""123456789""/>
                </identifier>
                <qualification>
                    <code>
                        <coding>
                            <system value=""https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type""/>
                            <code value=""00""/>
                        </coding>
                    </code>
                </qualification>
            </Practitioner>
        </resource>
    </entry>
    <entry>
        <resource>
            <Patient>
                <id value=""patient-1""/>
                <identifier>
                    <system value=""http://fhir.de/sid/gkv/kvid-10""/>
                    <value value=""A123456789""/>
                </identifier>
                <name>
                    <family value=""Mustermann""/>
                    <given value=""Max""/>
                </name>
                <birthDate value=""1980-01""/>
            </Patient>
        </resource>
    </entry>
</Bundle>";
        }

        private static string GetProjectRoot()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectRoot = currentDirectory;
            
            // Try to find the project root by looking for specific files
            while (!string.IsNullOrEmpty(projectRoot) && 
                   !File.Exists(Path.Combine(projectRoot, "ERezeptExtractor.csproj")))
            {
                var parent = Directory.GetParent(projectRoot);
                if (parent == null) break;
                projectRoot = parent.FullName;
            }
            
            return projectRoot ?? currentDirectory;
        }
    }
}
