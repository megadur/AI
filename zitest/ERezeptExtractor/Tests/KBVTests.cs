using Xunit;
using ERezeptExtractor.KBV;
using ERezeptExtractor.Models;
using ERezeptExtractor.Validation;

namespace ERezeptExtractor.Tests
{
    public class KBVPractitionerExtractorTests
    {
        private const string SampleXmlPath = @"d:\data\code\_examples\AI\zitest\eRezeptAbgabedaten.xml";
        
        [Fact]
        public void ExtractKBVData_ValidXmlFile_ReturnsExtendedData()
        {
            // Arrange
            var extractor = new KBVPractitionerExtractor();
            
            // Act
            var result = extractor.ExtractKBVData(SampleXmlPath);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Practitioner);
            Assert.NotNull(result.Bundle);
            Assert.NotNull(result.Pharmacy);
            Assert.NotNull(result.Invoice);
        }

        [Fact]
        public void ExtractKBVData_ValidXmlFile_ExtractsPractitionerData()
        {
            // Arrange
            var extractor = new KBVPractitionerExtractor();
            
            // Act
            var result = extractor.ExtractKBVData(SampleXmlPath);
            
            // Assert
            Assert.NotNull(result.Practitioner);
            Assert.NotNull(result.Practitioner.Name);
            Assert.False(string.IsNullOrEmpty(result.Practitioner.LANR));
            Assert.NotEmpty(result.Practitioner.Qualifications);
        }

        [Fact]
        public void ApplyKBVLANRLogic_WithAssistantAndResponsible_ReturnsAssistantLANR()
        {
            // Arrange
            var practitioner = new PractitionerInfo
            {
                Name = new PractitionerNameInfo { Family = "Test", Given = "Doctor" },
                Qualifications = new List<QualificationInfo>
                {
                    new() { TypeCode = "03", TypeDisplay = "Assistant", AssociatedLANR = "123456789" },
                    new() { TypeCode = "00", TypeDisplay = "Responsible", AssociatedLANR = "987654321" }
                }
            };

            var extractor = new KBVPractitionerExtractor();

            // Act
            var result = extractor.ApplyKBVLANRLogic(practitioner);

            // Assert
            Assert.Equal("123456789", result.LANR);
            Assert.Equal("987654321", result.LANR_Responsible);
        }

        [Fact]
        public void ApplyKBVLANRLogic_WithOnlyResponsible_ReturnsResponsibleLANR()
        {
            // Arrange
            var practitioner = new PractitionerInfo
            {
                Name = new PractitionerNameInfo { Family = "Test", Given = "Doctor" },
                Qualifications = new List<QualificationInfo>
                {
                    new() { TypeCode = "00", TypeDisplay = "Responsible", AssociatedLANR = "987654321" }
                }
            };

            var extractor = new KBVPractitionerExtractor();

            // Act
            var result = extractor.ApplyKBVLANRLogic(practitioner);

            // Assert
            Assert.Equal("987654321", result.LANR);
            Assert.Null(result.LANR_Responsible);
        }

        [Fact]
        public void ApplyKBVLANRLogic_WithNoValidLANR_ReturnsDefault()
        {
            // Arrange
            var practitioner = new PractitionerInfo
            {
                Name = new PractitionerNameInfo { Family = "Test", Given = "Doctor" },
                Qualifications = new List<QualificationInfo>
                {
                    new() { TypeCode = "05", TypeDisplay = "Other", AssociatedLANR = "" }
                }
            };

            var extractor = new KBVPractitionerExtractor();

            // Act
            var result = extractor.ApplyKBVLANRLogic(practitioner);

            // Assert
            Assert.Equal("000000000", result.LANR);
            Assert.Null(result.LANR_Responsible);
        }

        [Fact]
        public void ApplyKBVLANRLogic_WithResponsibleType04_ReturnsCorrectLANR()
        {
            // Arrange
            var practitioner = new PractitionerInfo
            {
                Name = new PractitionerNameInfo { Family = "Test", Given = "Doctor" },
                Qualifications = new List<QualificationInfo>
                {
                    new() { TypeCode = "04", TypeDisplay = "Responsible Doctor", AssociatedLANR = "111222333" }
                }
            };

            var extractor = new KBVPractitionerExtractor();

            // Act
            var result = extractor.ApplyKBVLANRLogic(practitioner);

            // Assert
            Assert.Equal("111222333", result.LANR);
            Assert.Null(result.LANR_Responsible);
        }

        [Fact]
        public void ApplyKBVLANRLogic_WithMultipleAssistants_TakesFirst()
        {
            // Arrange
            var practitioner = new PractitionerInfo
            {
                Name = new PractitionerNameInfo { Family = "Test", Given = "Doctor" },
                Qualifications = new List<QualificationInfo>
                {
                    new() { TypeCode = "03", TypeDisplay = "Assistant 1", AssociatedLANR = "111111111" },
                    new() { TypeCode = "03", TypeDisplay = "Assistant 2", AssociatedLANR = "222222222" },
                    new() { TypeCode = "00", TypeDisplay = "Responsible", AssociatedLANR = "333333333" }
                }
            };

            var extractor = new KBVPractitionerExtractor();

            // Act
            var result = extractor.ApplyKBVLANRLogic(practitioner);

            // Assert
            Assert.Equal("111111111", result.LANR);
            Assert.Equal("333333333", result.LANR_Responsible);
        }
    }

    public class KBVPractitionerValidatorTests
    {
        [Fact]
        public void ValidateKBVData_ValidData_ReturnsNoErrors()
        {
            // Arrange
            var data = CreateValidExtendedData();

            // Act
            var errors = KBVPractitionerValidator.ValidateKBVData(data);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateKBVData_MissingPractitioner_ReturnsError()
        {
            // Arrange
            var data = CreateValidExtendedData();
            data.Practitioner = null;

            // Act
            var errors = KBVPractitionerValidator.ValidateKBVData(data);

            // Assert
            Assert.Contains("Practitioner information is missing", errors);
        }

        [Fact]
        public void ValidateLANR_InvalidLength_ReturnsError()
        {
            // Arrange
            var data = CreateValidExtendedData();
            data.Practitioner.LANR = "12345"; // Too short

            // Act
            var errors = KBVPractitionerValidator.ValidateKBVData(data);

            // Assert
            Assert.Contains(errors, e => e.Contains("LANR must be exactly 9 characters long"));
        }

        [Fact]
        public void ValidateLANR_NonAlphanumeric_ReturnsError()
        {
            // Arrange
            var data = CreateValidExtendedData();
            data.Practitioner.LANR = "123-456-78"; // Contains dashes

            // Act
            var errors = KBVPractitionerValidator.ValidateKBVData(data);

            // Assert
            Assert.Contains(errors, e => e.Contains("LANR must be alphanumeric only"));
        }

        [Fact]
        public void ValidateQualifications_UnknownType_ReturnsError()
        {
            // Arrange
            var data = CreateValidExtendedData();
            data.Practitioner.Qualifications.Add(new QualificationInfo 
            { 
                TypeCode = "99", 
                TypeDisplay = "Unknown" 
            });

            // Act
            var errors = KBVPractitionerValidator.ValidateKBVData(data);

            // Assert
            Assert.Contains(errors, e => e.Contains("Unknown qualification type code: 99"));
        }

        [Fact]
        public void ValidateLANRLogic_AssistantWithoutResponsible_ReturnsError()
        {
            // Arrange
            var data = CreateValidExtendedData();
            data.Practitioner.Qualifications.Clear();
            data.Practitioner.Qualifications.Add(new QualificationInfo 
            { 
                TypeCode = "03", 
                TypeDisplay = "Assistant",
                AssociatedLANR = "123456789"
            });
            data.Practitioner.LANR_Responsible = null;

            // Act
            var errors = KBVPractitionerValidator.ValidateKBVData(data);

            // Assert
            Assert.Contains(errors, e => e.Contains("When assistant qualification is present, LANR_Responsible should be filled"));
        }

        [Theory]
        [InlineData("00", "Arzt (Doctor)")]
        [InlineData("03", "Assistenz (Assistant)")]
        [InlineData("04", "Verantwortlicher Arzt (Responsible Doctor)")]
        [InlineData("99", "Unknown type: 99")]
        public void GetQualificationTypeDescription_ReturnsCorrectDescription(string typeCode, string expectedDescription)
        {
            // Act
            var description = KBVPractitionerValidator.GetQualificationTypeDescription(typeCode);

            // Assert
            Assert.Equal(expectedDescription, description);
        }

        private static ExtendedERezeptData CreateValidExtendedData()
        {
            return new ExtendedERezeptData
            {
                BundleId = "test-bundle",
                PrescriptionId = "test-prescription",
                Timestamp = DateTime.Now,
                Pharmacy = new PharmacyInfo
                {
                    Name = "Test Pharmacy",
                    IK_Number = "123456789",
                    Address = new AddressInfo
                    {
                        Street = "Test Street 1",
                        City = "Test City",
                        PostalCode = "12345",
                        Country = "DE"
                    }
                },
                Invoice = new InvoiceInfo
                {
                    Id = "TEST-001",
                    Status = "completed",
                    TotalGross = 10.50m,
                    Currency = "EUR",
                    LineItems = new List<LineItemInfo>
                    {
                        new()
                        {
                            Sequence = 1,
                            PZN = "12345678",
                            Amount = 10.50m,
                            Currency = "EUR",
                            VatRate = 0.19m
                        }
                    }
                },
                Practitioner = new PractitionerInfo
                {
                    Name = new PractitionerNameInfo
                    {
                        Family = "Mustermann",
                        Given = "Max"
                    },
                    LANR = "123456789",
                    Qualifications = new List<QualificationInfo>
                    {
                        new()
                        {
                            TypeCode = "00",
                            TypeDisplay = "Arzt",
                            AssociatedLANR = "123456789"
                        }
                    }
                }
            };
        }
    }
}
