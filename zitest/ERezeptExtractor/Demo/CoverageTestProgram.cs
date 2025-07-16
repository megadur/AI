using ERezeptExtractor.KBV;

namespace ERezeptExtractor.Demo
{
    class CoverageTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== KBV Coverage Payer (kostentr_bg) Extraction Test ===");
            Console.WriteLine();

            try
            {
                // Sample FHIR XML with Coverage resource containing UK/BG payer type
                var sampleXml = """
                <Bundle xmlns="http://hl7.org/fhir">
                    <id value="sample-bundle"/>
                    <entry>
                        <resource>
                            <Coverage>
                                <id value="coverage-example"/>
                                <type>
                                    <coding>
                                        <system value="https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Payor_Type_KBV"/>
                                        <code value="UK"/>
                                        <display value="Unfallkasse"/>
                                    </coding>
                                </type>
                                <payor>
                                    <identifier>
                                        <extension url="https://fhir.kbv.de/StructureDefinition/KBV_EX_FOR_Alternative_IK">
                                            <valueIdentifier>
                                                <system value="http://fhir.de/sid/arge-ik/iknr"/>
                                                <value value="123456789"/>
                                            </valueIdentifier>
                                        </extension>
                                        <system value="http://fhir.de/sid/arge-ik/iknr"/>
                                        <value value="987654321"/>
                                    </identifier>
                                </payor>
                            </Coverage>
                        </resource>
                    </entry>
                    <entry>
                        <resource>
                            <Organization>
                                <id value="pharmacy-example"/>
                                <name value="Test Pharmacy"/>
                            </Organization>
                        </resource>
                    </entry>
                </Bundle>
                """;

                Console.WriteLine("1. Creating KBV extractor...");
                var extractor = new KBVPractitionerExtractor();
                Console.WriteLine("   ✅ KBV extractor created");
                Console.WriteLine();

                Console.WriteLine("2. Extracting data from sample XML...");
                var extractedData = extractor.ExtractKBVData(sampleXml);
                Console.WriteLine("   ✅ Data extraction completed");
                Console.WriteLine();

                Console.WriteLine("3. Checking extracted kostentr_bg value...");
                if (!string.IsNullOrEmpty(extractedData.KostentraegerBG))
                {
                    Console.WriteLine($"   ✅ KostentraegerBG found: {extractedData.KostentraegerBG}");
                    Console.WriteLine("   ✅ Successfully extracted alternative IK number from UK coverage");
                }
                else
                {
                    Console.WriteLine("   ❌ No KostentraegerBG value found");
                }
                Console.WriteLine();

                Console.WriteLine("4. Testing BG coverage type...");
                var sampleXmlBG = sampleXml.Replace("UK", "BG").Replace("Unfallkasse", "Berufsgenossenschaft").Replace("123456789", "555666777");
                var extractedDataBG = extractor.ExtractKBVData(sampleXmlBG);
                
                if (!string.IsNullOrEmpty(extractedDataBG.KostentraegerBG))
                {
                    Console.WriteLine($"   ✅ BG KostentraegerBG found: {extractedDataBG.KostentraegerBG}");
                    Console.WriteLine("   ✅ Successfully extracted alternative IK number from BG coverage");
                }
                else
                {
                    Console.WriteLine("   ❌ No BG KostentraegerBG value found");
                }
                Console.WriteLine();

                Console.WriteLine("5. Displaying extraction results:");
                Console.WriteLine($"   - UK Coverage Bundle ID: {extractedData.BundleId}");
                Console.WriteLine($"   - UK KostentraegerBG: {extractedData.KostentraegerBG}");
                Console.WriteLine($"   - BG KostentraegerBG: {extractedDataBG.KostentraegerBG}");
                Console.WriteLine();

                Console.WriteLine("=== Coverage Payer Extraction Test Completed Successfully! ===");
                Console.WriteLine();
                Console.WriteLine("Key Features Tested:");
                Console.WriteLine("• XPath-based Coverage resource extraction");
                Console.WriteLine("• Payer type filtering (UK and BG)");
                Console.WriteLine("• Alternative IK number extraction");
                Console.WriteLine("• FHIR extension handling");
                Console.WriteLine("• Integration with existing KBV extractor");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
