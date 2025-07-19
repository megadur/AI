using ERezeptVerordnungExtractor;
using ERezeptVerordnungExtractor.Models;
using System.Text.Json;

namespace ERezeptExtractor.Demo
{
    /// <summary>
    /// Demo program showing how to use the ERezeptVerordnungExtractor
    /// </summary>
    public class VerordnungDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== eRezept Verordnung (Prescription) Extractor Demo ===");
            Console.WriteLine();

            try
            {
                // Path to the sample Verordnung XML file
                var xmlFilePath = @"d:\data\code\_examples\AI\zitest\bundle_Verordnungsdaten.xml";
                
                if (!File.Exists(xmlFilePath))
                {
                    Console.WriteLine($"Sample XML file not found: {xmlFilePath}");
                    return;
                }

                // Create extractor instance
                var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

                // Extract data from XML file
                Console.WriteLine("Extracting data from Verordnung XML...");
                var data = extractor.ExtractFromFile(xmlFilePath);

                // Display extracted data
                DisplayExtractedData(data);

                // Serialize to JSON for demonstration
                Console.WriteLine("\n=== JSON Serialization ===");
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(data, jsonOptions);
                Console.WriteLine(json);

                Console.WriteLine("\n=== Demo completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during extraction: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void DisplayExtractedData(ERezeptVerordnungData data)
        {
            Console.WriteLine("=== Extracted Prescription Data ===");
            Console.WriteLine();

            // Bundle Information
            Console.WriteLine("Bundle Information:");
            Console.WriteLine($"  Bundle ID: {data.BundleId}");
            Console.WriteLine($"  Prescription ID: {data.PrescriptionId}");
            Console.WriteLine($"  Timestamp: {data.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Practitioner (Doctor) Information
            Console.WriteLine("Practitioner (Doctor) Information:");
            Console.WriteLine($"  ID: {data.Practitioner.Id}");
            Console.WriteLine($"  LANR: {data.Practitioner.LANR}");
            Console.WriteLine($"  Name: {data.Practitioner.Name.FullName}");
            Console.WriteLine($"  Qualifications: {data.Practitioner.Qualifications.Count} qualification(s)");
            foreach (var qual in data.Practitioner.Qualifications)
            {
                Console.WriteLine($"    - Type: {qual.TypeCode}, Display: {qual.TypeDisplay}, Text: {qual.Text}");
            }
            Console.WriteLine();

            // Organization (Practice) Information
            Console.WriteLine("Organization (Practice) Information:");
            Console.WriteLine($"  ID: {data.Organization.Id}");
            Console.WriteLine($"  BSNR: {data.Organization.BSNR}");
            Console.WriteLine($"  Name: {data.Organization.Name}");
            Console.WriteLine($"  Address: {data.Organization.Address.FullAddress}");
            Console.WriteLine($"  Contact methods: {data.Organization.Telecoms.Count}");
            foreach (var telecom in data.Organization.Telecoms)
            {
                Console.WriteLine($"    - {telecom.System}: {telecom.Value}");
            }
            Console.WriteLine();

            // Patient Information
            Console.WriteLine("Patient Information:");
            Console.WriteLine($"  ID: {data.Patient.Id}");
            Console.WriteLine($"  Insurance Number: {data.Patient.InsuranceNumber}");
            Console.WriteLine($"  Name: {data.Patient.Name.FullName}");
            Console.WriteLine($"  Birth Date: {data.Patient.BirthDate:yyyy-MM-dd}");
            Console.WriteLine($"  Address: {data.Patient.Address.FullAddress}");
            Console.WriteLine();

            // Coverage (Insurance) Information
            Console.WriteLine("Coverage (Insurance) Information:");
            Console.WriteLine($"  ID: {data.Coverage.Id}");
            Console.WriteLine($"  Status: {data.Coverage.Status}");
            Console.WriteLine($"  Type: {data.Coverage.Type}");
            Console.WriteLine($"  Payor IK: {data.Coverage.PayorIK}");
            Console.WriteLine($"  Alternative IK: {data.Coverage.AlternativeIK}");
            Console.WriteLine($"  Payor Name: {data.Coverage.PayorName}");
            Console.WriteLine($"  Personen Gruppe: {data.Coverage.PersonenGruppe}");
            Console.WriteLine($"  DMP Kennzeichen: {data.Coverage.DmpKennzeichen}");
            Console.WriteLine($"  Versicherten Art: {data.Coverage.VersichertenArt}");
            Console.WriteLine();

            // Medication Information
            Console.WriteLine("Medication Information:");
            Console.WriteLine($"  ID: {data.Medication.Id}");
            Console.WriteLine($"  PZN: {data.Medication.PZN}");
            Console.WriteLine($"  Name: {data.Medication.Name}");
            Console.WriteLine($"  Form: {data.Medication.Form}");
            Console.WriteLine($"  Category: {data.Medication.Category}");
            Console.WriteLine($"  Norm Size: {data.Medication.NormSize}");
            Console.WriteLine($"  Is Vaccine: {data.Medication.IsVaccine}");
            Console.WriteLine($"  Medication Type: {data.Medication.MedicationType}");
            Console.WriteLine();

            // Medication Request (Prescription) Information
            Console.WriteLine("Medication Request (Prescription) Information:");
            Console.WriteLine($"  ID: {data.MedicationRequest.Id}");
            Console.WriteLine($"  Status: {data.MedicationRequest.Status}");
            Console.WriteLine($"  Intent: {data.MedicationRequest.Intent}");
            Console.WriteLine($"  Authored On: {data.MedicationRequest.AuthoredOn:yyyy-MM-dd}");
            Console.WriteLine($"  Status Co-Payment: {data.MedicationRequest.StatusCoPayment}");
            Console.WriteLine($"  Emergency Services Fee: {data.MedicationRequest.EmergencyServicesFee}");
            Console.WriteLine($"  BVG: {data.MedicationRequest.BVG}");
            
            // Accident Information
            if (!string.IsNullOrEmpty(data.MedicationRequest.Accident.Kennzeichen))
            {
                Console.WriteLine("  Accident Information:");
                Console.WriteLine($"    Kennzeichen: {data.MedicationRequest.Accident.Kennzeichen}");
                Console.WriteLine($"    Betrieb: {data.MedicationRequest.Accident.Betrieb}");
                Console.WriteLine($"    Unfalltag: {data.MedicationRequest.Accident.Unfalltag:yyyy-MM-dd}");
            }

            // Multiple Prescription
            Console.WriteLine($"  Multiple Prescription: {data.MedicationRequest.Multiple.Kennzeichen}");

            // Dosage
            Console.WriteLine("  Dosage Information:");
            Console.WriteLine($"    Dosage Flag: {data.MedicationRequest.Dosage.DosageFlag}");
            Console.WriteLine($"    Text: {data.MedicationRequest.Dosage.Text}");

            // Dispense Request
            Console.WriteLine("  Dispense Request:");
            Console.WriteLine($"    Quantity: {data.MedicationRequest.DispenseRequest.Quantity} {data.MedicationRequest.DispenseRequest.Unit}");

            // Substitution
            Console.WriteLine($"  Substitution Allowed: {data.MedicationRequest.Substitution.Allowed}");
        }
    }
}
