using Xunit;
using ERezeptVerordnungExtractor;
using ERezeptVerordnungExtractor.Models;

namespace ERezeptExtractor.Tests
{
    public class ERezeptVerordnungExtractorTests
    {
        private const string SampleXmlPath = @"d:\data\code\_examples\AI\zitest\bundle_Verordnungsdaten.xml";

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ReturnsData()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.BundleId);
            Assert.NotEmpty(result.PrescriptionId);
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsBundleInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.Equal("0f1be812-21cc-45a9-a4b0-0df2ad9a15b4", result.BundleId);
            Assert.Equal("160.000.200.744.243.73", result.PrescriptionId);
            Assert.True(result.Timestamp != default(DateTime));
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsPractitionerInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result.Practitioner);
            Assert.Equal("d4be814f-692b-4b8c-9aab-2a0ed6500b21", result.Practitioner.Id);
            Assert.Equal("777426306", result.Practitioner.LANR);
            Assert.Equal("Greese", result.Practitioner.Name.Family);
            Assert.Equal("Ralf", result.Practitioner.Name.Given);
            Assert.Equal("Dr.med.", result.Practitioner.Name.Prefix);
            Assert.NotEmpty(result.Practitioner.Qualifications);
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsOrganizationInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result.Organization);
            Assert.Equal("885daf96-a18d-4fab-9b7c-73942167c81b", result.Organization.Id);
            Assert.Equal("830164500", result.Organization.BSNR);
            Assert.Equal("Dr. Ralf Greese", result.Organization.Name);
            Assert.NotEmpty(result.Organization.Telecoms);
            Assert.NotNull(result.Organization.Address);
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsPatientInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result.Patient);
            Assert.Equal("66864bfc-0601-49c8-9d0c-e846fef4cb64", result.Patient.Id);
            Assert.Equal("M415602272", result.Patient.InsuranceNumber);
            Assert.Equal("Friske", result.Patient.Name.Family);
            Assert.Equal("Keven", result.Patient.Name.Given);
            Assert.True(result.Patient.BirthDate.HasValue);
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsCoverageInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result.Coverage);
            Assert.Equal("4fdaa45e-7a86-42e4-a8f4-5ad2c10fee10", result.Coverage.Id);
            Assert.Equal("active", result.Coverage.Status);
            Assert.Equal("BG", result.Coverage.Type);
            Assert.Equal("120890837", result.Coverage.PayorIK);
            Assert.Equal("120890837", result.Coverage.AlternativeIK);
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsMedicationInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result.Medication);
            Assert.Equal("d84bd2cc-2947-4ef1-b719-183ef1966e01", result.Medication.Id);
            Assert.Equal("06313409", result.Medication.PZN);
            Assert.Equal("Ibuflam 600mg Lichtenstein", result.Medication.Name);
            Assert.Equal("FTA", result.Medication.Form);
            Assert.Equal("N2", result.Medication.NormSize);
            Assert.False(result.Medication.IsVaccine);
        }

        [Fact]
        public void ExtractFromFile_ValidVerordnungXml_ExtractsMedicationRequestInfo()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act
            var result = extractor.ExtractFromFile(SampleXmlPath);

            // Assert
            Assert.NotNull(result.MedicationRequest);
            Assert.Equal("6dd3591f-d617-4a2b-beea-bf2a8c5b9bbe", result.MedicationRequest.Id);
            Assert.Equal("active", result.MedicationRequest.Status);
            Assert.Equal("order", result.MedicationRequest.Intent);
            Assert.Equal("1", result.MedicationRequest.StatusCoPayment);
            Assert.False(result.MedicationRequest.EmergencyServicesFee);
            Assert.False(result.MedicationRequest.BVG);
            
            // Test accident info
            Assert.NotNull(result.MedicationRequest.Accident);
            Assert.Equal("2", result.MedicationRequest.Accident.Kennzeichen);
            Assert.Equal("Fresh Fruits GmbH", result.MedicationRequest.Accident.Betrieb);
            
            // Test dosage
            Assert.NotNull(result.MedicationRequest.Dosage);
            Assert.True(result.MedicationRequest.Dosage.DosageFlag);
            Assert.Contains("3 x 1", result.MedicationRequest.Dosage.Text);
            
            // Test dispense request
            Assert.NotNull(result.MedicationRequest.DispenseRequest);
            Assert.Equal(1.0m, result.MedicationRequest.DispenseRequest.Quantity);
            Assert.Equal("{Package}", result.MedicationRequest.DispenseRequest.Unit);
            
            // Test substitution
            Assert.NotNull(result.MedicationRequest.Substitution);
            Assert.True(result.MedicationRequest.Substitution.Allowed);
        }

        [Fact]
        public void ExtractFromXml_NullContent_ThrowsException()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => extractor.ExtractFromXml(null));
        }

        [Fact]
        public void ExtractFromFile_NonExistentFile_ThrowsException()
        {
            // Arrange
            var extractor = new ERezeptVerordnungExtractor.ERezeptVerordnungExtractor();

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => extractor.ExtractFromFile("nonexistent.xml"));
        }
    }
}
