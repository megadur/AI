using System;
using System.IO;
using ERezeptExtractor;

namespace ERezeptExtractor.Demo
{
    /// <summary>
    /// Simple console demonstration of AVD-based FHIR extraction
    /// </summary>
    class SimpleAVDDemo
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== AVD Specification-Based FHIR Extraction Demo ===\n");

            try
            {
                // Initialize the extractor
                var extractor = new AVDBasedExtractor();

                // Define paths
                var projectRoot = GetProjectRoot();
                var specificationPath = Path.Combine(projectRoot, "doku", "AVD_Daten - Tabellenblatt2.csv");
                
                Console.WriteLine($"Looking for specification file: {specificationPath}");

                if (!File.Exists(specificationPath))
                {
                    Console.WriteLine("❌ AVD specification file not found!");
                    Console.WriteLine("Please ensure the CSV file is in the doku folder.");
                    Console.WriteLine("\nPress any key to continue with a simple demo...");
                    Console.ReadKey();
                    RunSimpleDemo(extractor);
                    return;
                }

                // Validate the specifications first
                Console.WriteLine("📋 Validating specifications...");
                var report = extractor.ValidateSpecifications(specificationPath);
                
                Console.WriteLine($"✅ Validation completed:");
                Console.WriteLine($"   - Total specifications: {report.TotalSpecifications}");
                Console.WriteLine($"   - Complex specifications: {report.ComplexSpecifications}");
                Console.WriteLine($"   - Profile groups: {report.ProfileGroups.Count}");
                Console.WriteLine($"   - Validation success: {report.Success}");

                if (report.ValidationIssues.Count > 0)
                {
                    Console.WriteLine($"   - Issues found: {report.ValidationIssues.Count}");
                    foreach (var issue in report.ValidationIssues.Take(3))
                    {
                        Console.WriteLine($"     • {issue}");
                    }
                    if (report.ValidationIssues.Count > 3)
                    {
                        Console.WriteLine($"     ... and {report.ValidationIssues.Count - 3} more");
                    }
                }

                // Analyze complex specifications
                Console.WriteLine("\n🔍 Analyzing complex specifications...");
                var complexSpecs = extractor.GetComplexSpecifications(specificationPath);
                Console.WriteLine($"Found {complexSpecs.Count} specifications with business logic:");

                foreach (var spec in complexSpecs.Take(5))
                {
                    Console.WriteLine($"   • {spec.Attribut} (ID: {spec.ID}): {spec.BeschreibungDesAttributs}");
                    if (spec.BusinessRules.Count > 0)
                    {
                        Console.WriteLine($"     Rules: {spec.BusinessRules.Count}, XPaths: {spec.XPathExpressions.Count}");
                    }
                }

                // Test with sample FHIR XML
                Console.WriteLine("\n🧪 Testing extraction with sample FHIR XML...");
                var sampleXml = CreateSampleFhirXml();
                
                var result = extractor.ExtractFromXml(sampleXml, specificationPath);
                
                Console.WriteLine($"\n📊 Extraction Results:");
                Console.WriteLine($"   - Success: {result.ExtractionSuccess}");
                Console.WriteLine($"   - Total values: {result.ExtractedData.ExtractedValues.Count}");
                Console.WriteLine($"   - Errors: {result.ExtractedData.Errors.Count}");
                Console.WriteLine($"   - Warnings: {result.ExtractedData.Warnings.Count}");

                // Show some extracted values
                var extractedValues = result.ExtractedData.ExtractedValues
                    .Where(kvp => kvp.Value != null && !string.IsNullOrEmpty(kvp.Value.ToString()))
                    .Take(10)
                    .ToList();

                if (extractedValues.Any())
                {
                    Console.WriteLine("\n✅ Successfully extracted values:");
                    foreach (var kvp in extractedValues)
                    {
                        Console.WriteLine($"   • {kvp.Key}: {kvp.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("\n⚠️  No values were extracted from the sample XML.");
                    Console.WriteLine("    This is expected with the minimal sample data.");
                }

                // Show some errors/warnings if any
                if (result.ExtractedData.Errors.Count > 0)
                {
                    Console.WriteLine($"\n⚠️  Sample errors (first 3):");
                    foreach (var error in result.ExtractedData.Errors.Take(3))
                    {
                        Console.WriteLine($"   • {error}");
                    }
                }

                Console.WriteLine("\n🎉 Demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void RunSimpleDemo(AVDBasedExtractor extractor)
        {
            Console.WriteLine("\n🎯 Running simple demo without specification file...");
            
            // Create a minimal specification for demo
            var tempSpecFile = CreateTempSpecification();
            var sampleXml = CreateSampleFhirXml();
            
            try
            {
                var result = extractor.ExtractFromXml(sampleXml, tempSpecFile);
                
                Console.WriteLine($"📊 Demo Results:");
                Console.WriteLine($"   - Specifications loaded: {result.Specifications.Count}");
                Console.WriteLine($"   - Extraction success: {result.ExtractionSuccess}");
                Console.WriteLine($"   - Values extracted: {result.ExtractedData.ExtractedValues.Count}");
                
                foreach (var kvp in result.ExtractedData.ExtractedValues)
                {
                    if (kvp.Value != null)
                    {
                        Console.WriteLine($"   • {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            finally
            {
                if (File.Exists(tempSpecFile))
                {
                    File.Delete(tempSpecFile);
                }
            }
        }

        private static string CreateTempSpecification()
        {
            var tempFile = Path.GetTempFileName();
            var csvContent = @"Attribut,ID ,Beschreibung des Attributs,Länge,Darstellung,Profile,Anpassung notwendig Zusätzl.,Partieller XPath
rezept_id,5,Dokumenten-ID,22,alphanumerisch,https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Bundle,,fhir:identifier[fhir:system/@value='https://gematik.de/fhir/erp/NamingSystem/GEM_ERP_NS_PrescriptionId']/fhir:value
pat_nachname,21,Nachname des Versicherten,..45,alphanumerisch,https://fhir.kbv.de/StructureDefinition/KBV_PR_FOR_Patient,,fhir:Patient/fhir:name/fhir:family
pat_vorname,20,Vorname des Versicherten,..45,alphanumerisch,https://fhir.kbv.de/StructureDefinition/KBV_PR_FOR_Patient,,fhir:Patient/fhir:name/fhir:given";

            File.WriteAllText(tempFile, csvContent);
            return tempFile;
        }

        private static string CreateSampleFhirXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Bundle xmlns=""http://hl7.org/fhir"">
    <id value=""sample-bundle-demo""/>
    <identifier>
        <system value=""https://gematik.de/fhir/erp/NamingSystem/GEM_ERP_NS_PrescriptionId""/>
        <value value=""160.123.456.789.123.58""/>
    </identifier>
    <entry>
        <resource>
            <Practitioner>
                <id value=""practitioner-demo""/>
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
                <id value=""patient-demo""/>
                <identifier>
                    <system value=""http://fhir.de/sid/gkv/kvid-10""/>
                    <value value=""A123456789""/>
                </identifier>
                <name>
                    <family value=""Mustermann""/>
                    <given value=""Max""/>
                </name>
                <birthDate value=""1980-01-15""/>
            </Patient>
        </resource>
    </entry>
    <entry>
        <resource>
            <Organization>
                <id value=""organization-demo""/>
                <identifier>
                    <system value=""https://fhir.kbv.de/NamingSystem/KBV_NS_Base_BSNR""/>
                    <value value=""123456789""/>
                </identifier>
            </Organization>
        </resource>
    </entry>
</Bundle>";
        }

        private static string GetProjectRoot()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectRoot = currentDirectory;
            
            // Try to find the project root by looking for specific files
            while (!string.IsNullOrEmpty(projectRoot))
            {
                if (File.Exists(Path.Combine(projectRoot, "ERezeptExtractor.csproj")) ||
                    Directory.Exists(Path.Combine(projectRoot, "doku")))
                {
                    break;
                }
                
                var parent = Directory.GetParent(projectRoot);
                if (parent == null) break;
                projectRoot = parent.FullName;
            }
            
            return projectRoot ?? currentDirectory;
        }
    }
}
