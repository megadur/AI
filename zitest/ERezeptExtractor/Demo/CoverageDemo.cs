using ERezeptExtractor.KBV;

Console.WriteLine("=== KBV Coverage Payer (kostentr_bg) Extraction Demo ===");
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
                <Coverage>
                    <id value="coverage-bg-example"/>
                    <type>
                        <coding>
                            <system value="https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Payor_Type_KBV"/>
                            <code value="BG"/>
                            <display value="Berufsgenossenschaft"/>
                        </coding>
                    </type>
                    <payor>
                        <identifier>
                            <extension url="https://fhir.kbv.de/StructureDefinition/KBV_EX_FOR_Alternative_IK">
                                <valueIdentifier>
                                    <system value="http://fhir.de/sid/arge-ik/iknr"/>
                                    <value value="555666777"/>
                                </valueIdentifier>
                            </extension>
                            <system value="http://fhir.de/sid/arge-ik/iknr"/>
                            <value value="111222333"/>
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
        Console.WriteLine("   ✅ Successfully extracted alternative IK number from UK/BG coverage");
    }
    else
    {
        Console.WriteLine("   ❌ No KostentraegerBG value found");
    }
    Console.WriteLine();

    Console.WriteLine("4. Displaying extraction results:");
    Console.WriteLine($"   - Bundle ID: {extractedData.BundleId}");
    Console.WriteLine($"   - KostentraegerBG: {extractedData.KostentraegerBG}");
    Console.WriteLine($"   - Practitioner LANR: {extractedData.Practitioner.LANR}");
    Console.WriteLine();

    Console.WriteLine("=== Coverage Payer Extraction Demo Completed Successfully! ===");
    Console.WriteLine();
    Console.WriteLine("Key Features Demonstrated:");
    Console.WriteLine("• XPath-based Coverage resource extraction");
    Console.WriteLine("• Payer type filtering (UK/BG)");
    Console.WriteLine("• Alternative IK number extraction");
    Console.WriteLine("• FHIR extension handling");
    Console.WriteLine("• Integration with existing KBV extractor");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
