using System;
using System.IO;
using ERezeptVerordnungExtractor;

class TestVerordnungExtractor
{
    static void Main()
    {
        Console.WriteLine("=== Testing ERezeptVerordnungExtractor ===");
        
        try
        {
            var xmlFilePath = @"d:\data\code\_examples\AI\zitest\bundle_Verordnungsdaten.xml";
            
            if (!File.Exists(xmlFilePath))
            {
                Console.WriteLine($"XML file not found: {xmlFilePath}");
                return;
            }
            
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();
            var data = extractor.ExtractFromFile(xmlFilePath);
            
            Console.WriteLine("✅ Extraction successful!");
            Console.WriteLine($"Bundle ID: {data.BundleId}");
            Console.WriteLine($"Prescription ID: {data.PrescriptionId}");
            Console.WriteLine($"Doctor: {data.Practitioner.Name.FullName}");
            Console.WriteLine($"Patient: {data.Patient.Name.FullName}");
            Console.WriteLine($"Medication: {data.Medication.Name}");
            
            Console.WriteLine("✅ Test passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}
