using ERezeptVerordnungExtractor;

namespace ERezeptExtractor.Demo
{
    /// <summary>
    /// Simple test program for the ERezeptVerordnungExtractor
    /// </summary>
    public class SimpleVerordnungTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Simple eRezept Verordnung Extractor Test ===");
            Console.WriteLine();

            try
            {
                var xmlFilePath = @"d:\data\code\_examples\AI\zitest\bundle_Verordnungsdaten.xml";
                
                if (!File.Exists(xmlFilePath))
                {
                    Console.WriteLine($"XML file not found: {xmlFilePath}");
                    return;
                }

                Console.WriteLine($"Processing file: {xmlFilePath}");
                
                var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();
                var data = extractor.ExtractFromFile(xmlFilePath);

                Console.WriteLine("✅ Extraction successful!");
                Console.WriteLine();
                Console.WriteLine("Key Information:");
                Console.WriteLine($"  Bundle ID: {data.BundleId}");
                Console.WriteLine($"  Prescription ID: {data.PrescriptionId}");
                Console.WriteLine($"  Doctor: {data.Practitioner.Name.FullName} (LANR: {data.Practitioner.LANR})");
                Console.WriteLine($"  Patient: {data.Patient.Name.FullName} (Born: {data.Patient.BirthDate:yyyy-MM-dd})");
                Console.WriteLine($"  Medication: {data.Medication.Name} (PZN: {data.Medication.PZN})");
                Console.WriteLine($"  Dosage: {data.MedicationRequest.Dosage.Text}");
                Console.WriteLine($"  Quantity: {data.MedicationRequest.DispenseRequest.Quantity} {data.MedicationRequest.DispenseRequest.Unit}");
                
                if (!string.IsNullOrEmpty(data.MedicationRequest.Accident.Betrieb))
                {
                    Console.WriteLine($"  Accident: {data.MedicationRequest.Accident.Betrieb} on {data.MedicationRequest.Accident.Unfalltag:yyyy-MM-dd}");
                }
                
                Console.WriteLine();
                Console.WriteLine("✅ Test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
