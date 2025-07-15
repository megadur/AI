using Xunit;
using ERezeptExtractor;
using ERezeptExtractor.Validation;
using ERezeptExtractor.Serialization;

namespace ERezeptExtractor.Tests
{
    public class ERezeptExtractorTests
    {
        private const string SampleXml = @"<Bundle xmlns=""http://hl7.org/fhir"">
  <id value=""72bd741c-7ad8-41d8-97c3-9aabbdd0f5b4"" />
  <identifier>
    <system value=""https://gematik.de/fhir/NamingSystem/PrescriptionID"" />
    <value value=""160.123.456.789.123.58"" />
  </identifier>
  <type value=""document"" />
  <timestamp value=""2020-02-04T15:30:00Z"" />
  <entry>
    <fullUrl value=""urn:uuid:11ba8a7b-79f6-4b7a-8a29-0524c9e0ba41"" />
    <resource>
      <Organization>
        <id value=""11ba8a7b-79f6-4b7a-8a29-0524c9e0ba41"" />
        <identifier>
          <system value=""http://fhir.de/NamingSystem/arge-ik/iknr"" />
          <value value=""123456789"" />
        </identifier>
        <name value=""Adler-Apotheke"" />
        <address>
          <type value=""physical"" />
          <line value=""Taunusstraße 89"">
            <extension url=""http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-streetName"">
              <valueString value=""Taunusstraße"" />
            </extension>
            <extension url=""http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-houseNumber"">
              <valueString value=""89"" />
            </extension>
          </line>
          <city value=""Langen"" />
          <postalCode value=""63225"" />
          <country value=""DE"" />
        </address>
      </Organization>
    </resource>
  </entry>
</Bundle>";

        [Fact]
        public void ExtractFromXml_ShouldExtractBasicBundleInfo()
        {
            // Arrange
            var extractor = new ERezeptExtractor();

            // Act
            var result = extractor.ExtractFromXml(SampleXml);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("72bd741c-7ad8-41d8-97c3-9aabbdd0f5b4", result.BundleId);
            Assert.Equal("160.123.456.789.123.58", result.PrescriptionId);
            Assert.Equal(new DateTime(2020, 2, 4, 15, 30, 0, DateTimeKind.Utc), result.Timestamp);
        }

        [Fact]
        public void ExtractFromXml_ShouldExtractPharmacyInfo()
        {
            // Arrange
            var extractor = new ERezeptExtractor();

            // Act
            var result = extractor.ExtractFromXml(SampleXml);

            // Assert
            Assert.NotNull(result.Pharmacy);
            Assert.Equal("11ba8a7b-79f6-4b7a-8a29-0524c9e0ba41", result.Pharmacy.Id);
            Assert.Equal("123456789", result.Pharmacy.IK_Number);
            Assert.Equal("Adler-Apotheke", result.Pharmacy.Name);
            
            Assert.NotNull(result.Pharmacy.Address);
            Assert.Equal("Taunusstraße", result.Pharmacy.Address.Street);
            Assert.Equal("89", result.Pharmacy.Address.HouseNumber);
            Assert.Equal("Langen", result.Pharmacy.Address.City);
            Assert.Equal("63225", result.Pharmacy.Address.PostalCode);
            Assert.Equal("DE", result.Pharmacy.Address.Country);
        }

        [Fact]
        public void ExtractFromXml_WithNullInput_ShouldThrowArgumentException()
        {
            // Arrange
            var extractor = new ERezeptExtractor();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => extractor.ExtractFromXml(null!));
            Assert.Throws<ArgumentException>(() => extractor.ExtractFromXml(""));
            Assert.Throws<ArgumentException>(() => extractor.ExtractFromXml("   "));
        }

        [Fact]
        public void IsValidPZN_ShouldValidateCorrectly()
        {
            // Act & Assert
            Assert.True(ERezeptValidator.IsValidPZN("05454378"));
            Assert.True(ERezeptValidator.IsValidPZN("12345678"));
            
            Assert.False(ERezeptValidator.IsValidPZN("1234567"));   // Too short
            Assert.False(ERezeptValidator.IsValidPZN("123456789")); // Too long
            Assert.False(ERezeptValidator.IsValidPZN("1234567a"));  // Contains letter
            Assert.False(ERezeptValidator.IsValidPZN(""));          // Empty
            Assert.False(ERezeptValidator.IsValidPZN(null!));       // Null
        }

        [Fact]
        public void IsValidIKNumber_ShouldValidateCorrectly()
        {
            // Act & Assert
            Assert.True(ERezeptValidator.IsValidIKNumber("123456789"));
            
            Assert.False(ERezeptValidator.IsValidIKNumber("12345678"));  // Too short
            Assert.False(ERezeptValidator.IsValidIKNumber("1234567890")); // Too long
            Assert.False(ERezeptValidator.IsValidIKNumber("12345678a"));  // Contains letter
            Assert.False(ERezeptValidator.IsValidIKNumber(""));           // Empty
            Assert.False(ERezeptValidator.IsValidIKNumber(null!));        // Null
        }

        [Fact]
        public void ToJson_ShouldSerializeCorrectly()
        {
            // Arrange
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromXml(SampleXml);

            // Act
            var json = ERezeptSerializer.ToJson(data);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("72bd741c-7ad8-41d8-97c3-9aabbdd0f5b4", json);
            Assert.Contains("Adler-Apotheke", json);
            Assert.Contains("123456789", json);
        }

        [Fact]
        public void FromJson_ShouldDeserializeCorrectly()
        {
            // Arrange
            var extractor = new ERezeptExtractor();
            var originalData = extractor.ExtractFromXml(SampleXml);
            var json = ERezeptSerializer.ToJson(originalData);

            // Act
            var deserializedData = ERezeptSerializer.FromJson(json);

            // Assert
            Assert.NotNull(deserializedData);
            Assert.Equal(originalData.BundleId, deserializedData.BundleId);
            Assert.Equal(originalData.PrescriptionId, deserializedData.PrescriptionId);
            Assert.Equal(originalData.Pharmacy.Name, deserializedData.Pharmacy.Name);
        }

        [Fact]
        public void CreateSummaryReport_ShouldGenerateReport()
        {
            // Arrange
            var extractor = new ERezeptExtractor();
            var data = extractor.ExtractFromXml(SampleXml);

            // Act
            var report = ERezeptSerializer.CreateSummaryReport(data);

            // Assert
            Assert.NotNull(report);
            Assert.Contains("eRezept Data Summary", report);
            Assert.Contains("Bundle ID: 72bd741c-7ad8-41d8-97c3-9aabbdd0f5b4", report);
            Assert.Contains("Adler-Apotheke", report);
        }
    }
}
