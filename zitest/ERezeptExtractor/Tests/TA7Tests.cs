using Xunit;
using ERezeptExtractor.TA7;
using System.Xml.Linq;

namespace ERezeptExtractor.Tests
{
    public class TA7FhirReportGeneratorTests
    {
        private readonly TA7FhirReportGenerator _generator;
        private readonly TA7ReportValidator _validator;

        public TA7FhirReportGeneratorTests()
        {
            _generator = new TA7FhirReportGenerator();
            _validator = new TA7ReportValidator();
        }

        [Fact]
        public void CreateSampleReport_ReturnsValidReport()
        {
            // Act
            var report = _generator.CreateSampleReport();

            // Assert
            Assert.NotNull(report);
            Assert.NotNull(report.BundleId);
            Assert.NotNull(report.Identifier);
            Assert.NotNull(report.Composition);
            Assert.NotEmpty(report.Entries);
        }

        [Fact]
        public void GenerateTA7Report_WithValidReport_ReturnsValidXml()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var xml = _generator.GenerateTA7Report(report);

            // Assert
            Assert.NotNull(xml);
            Assert.Contains("Bundle", xml);
            Assert.Contains("http://hl7.org/fhir", xml);
            
            // Verify XML can be parsed
            var document = XDocument.Parse(xml);
            Assert.Equal("Bundle", document.Root?.Name.LocalName);
        }

        [Fact]
        public void GenerateTA7Report_WithNullReport_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _generator.GenerateTA7Report(null));
        }

        [Fact]
        public void GenerateTA7Report_SampleReport_PassesValidation()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var xml = _generator.GenerateTA7Report(report);
            var modelErrors = _validator.ValidateReport(report);
            var xmlErrors = _validator.ValidateXml(xml);

            // Assert
            Assert.Empty(modelErrors);
            Assert.Empty(xmlErrors);
        }

        [Fact]
        public void GenerateTA7Report_ContainsRequiredElements()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var xml = _generator.GenerateTA7Report(report);
            var document = XDocument.Parse(xml);
            var bundle = document.Root;

            // Assert
            Assert.NotNull(bundle?.Element("id"));
            Assert.NotNull(bundle?.Element("meta"));
            Assert.NotNull(bundle?.Element("identifier"));
            Assert.NotNull(bundle?.Element("type"));
            Assert.NotNull(bundle?.Element("timestamp"));
            Assert.True(bundle?.Elements("entry").Any());
        }

        [Fact]
        public void GenerateTA7Report_ContainsCorrectProfiles()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var xml = _generator.GenerateTA7Report(report);
            var document = XDocument.Parse(xml);

            // Assert
            var bundleProfile = document.Root?.Element("meta")?.Element("profile")?.Attribute("value")?.Value;
            Assert.Contains("GKVSV_PR_TA7_Rechnung_Bundle", bundleProfile);

            var compositionEntry = document.Root?.Elements("entry")
                .FirstOrDefault(e => e.Element("resource")?.Element("Composition") != null);
            var compositionProfile = compositionEntry?.Element("resource")?.Element("Composition")
                ?.Element("meta")?.Element("profile")?.Attribute("value")?.Value;
            Assert.Contains("GKVSV_PR_TA7_Rechnung_Composition", compositionProfile);
        }

        [Fact]
        public void GenerateTA7Report_WithCustomData_GeneratesCorrectXml()
        {
            // Arrange
            var customReport = new TA7ReportModels.TA7Report
            {
                BundleId = "test-bundle-id",
                ProfileVersion = "1.3",
                Timestamp = new DateTime(2024, 12, 15, 10, 30, 0),
                Identifier = new TA7ReportModels.TA7Identifier
                {
                    Dateinummer = "12345",
                    Value = "TESTFH24001"
                },
                Composition = new TA7ReportModels.TA7Composition
                {
                    Id = "test-composition-id",
                    IKEmpfaenger = "123456789",
                    IKKostentraeger = "987654321",
                    Rechnungsnummer = "123456789-24-0001",
                    InvoiceDate = new DateTime(2024, 12, 15),
                    Rechnungsdatum = new DateTime(2024, 12, 16),
                    AuthorIK = "555666777"
                },
                Entries = new List<TA7ReportModels.TA7Entry>()
            };

            // Act
            var xml = _generator.GenerateTA7Report(customReport);
            var document = XDocument.Parse(xml);

            // Assert
            Assert.Equal("test-bundle-id", document.Root?.Element("id")?.Attribute("value")?.Value);
            Assert.Equal("TESTFH24001", document.Root?.Element("identifier")?.Element("value")?.Attribute("value")?.Value);
            Assert.Equal("12345", document.Root?.Element("identifier")?.Element("extension")?.Element("valueString")?.Attribute("value")?.Value);
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("1.1")]
        [InlineData("1.2")]
        [InlineData("1.3")]
        public void GenerateTA7Report_WithDifferentProfileVersions_GeneratesValidXml(string profileVersion)
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.ProfileVersion = profileVersion;

            // Act
            var xml = _generator.GenerateTA7Report(report);

            // Assert
            Assert.NotNull(xml);
            Assert.Contains($"|{profileVersion}", xml);
            var document = XDocument.Parse(xml);
            Assert.NotNull(document.Root);
        }

        [Fact]
        public void GenerateTA7Report_WithInvoiceData_ContainsInvoiceElements()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            var invoiceEntry = report.Entries.FirstOrDefault(e => e.Resource.Type == "Invoice");
            Assert.NotNull(invoiceEntry);

            // Act
            var xml = _generator.GenerateTA7Report(report);
            var document = XDocument.Parse(xml);

            // Assert
            var invoiceElement = document.Descendants("Invoice").FirstOrDefault();
            Assert.NotNull(invoiceElement);
            Assert.NotNull(invoiceElement.Element("id"));
            Assert.NotNull(invoiceElement.Element("identifier"));
            Assert.NotNull(invoiceElement.Element("status"));
            Assert.NotNull(invoiceElement.Element("issuer"));
        }

        [Fact]
        public void GenerateTA7Report_WithBinaryData_ContainsBinaryElements()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var xml = _generator.GenerateTA7Report(report);
            var document = XDocument.Parse(xml);

            // Assert
            var binaryElements = document.Descendants("Binary").ToList();
            Assert.NotEmpty(binaryElements);

            foreach (var binary in binaryElements)
            {
                Assert.NotNull(binary.Element("id"));
                Assert.NotNull(binary.Element("contentType"));
                Assert.NotNull(binary.Element("data"));
                Assert.Equal("application/pkcs7-mime", binary.Element("contentType")?.Attribute("value")?.Value);
            }
        }

        [Fact]
        public void GenerateTA7Report_WithLineItems_ContainsPriceComponents()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var xml = _generator.GenerateTA7Report(report);
            var document = XDocument.Parse(xml);

            // Assert
            var priceComponents = document.Descendants("priceComponent").ToList();
            Assert.NotEmpty(priceComponents);

            foreach (var priceComponent in priceComponents)
            {
                Assert.NotNull(priceComponent.Element("type"));
                Assert.NotNull(priceComponent.Element("code"));
                Assert.NotNull(priceComponent.Element("amount"));
                
                var currency = priceComponent.Element("amount")?.Element("currency")?.Attribute("value")?.Value;
                Assert.Equal("EUR", currency);
            }
        }
    }

    public class TA7ReportValidatorTests
    {
        private readonly TA7ReportValidator _validator;
        private readonly TA7FhirReportGenerator _generator;

        public TA7ReportValidatorTests()
        {
            _validator = new TA7ReportValidator();
            _generator = new TA7FhirReportGenerator();
        }

        [Fact]
        public void ValidateReport_WithValidReport_ReturnsNoErrors()
        {
            // Arrange
            var report = _generator.CreateSampleReport();

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateReport_WithNullReport_ReturnsError()
        {
            // Act
            var errors = _validator.ValidateReport(null);

            // Assert
            Assert.Contains("Report cannot be null", errors);
        }

        [Fact]
        public void ValidateReport_WithInvalidBundleId_ReturnsError()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.BundleId = "invalid-id";

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains(errors, e => e.Contains("Bundle ID must be a valid GUID"));
        }

        [Fact]
        public void ValidateReport_WithInvalidIKNumber_ReturnsError()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.IKEmpfaenger = "123"; // Too short

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains(errors, e => e.Contains("Invalid IK Empfänger format"));
        }

        [Fact]
        public void ValidateReport_WithInvalidRechnungsnummer_ReturnsError()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.Rechnungsnummer = "invalid-format";

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains(errors, e => e.Contains("Invalid Rechnungsnummer format"));
        }

        [Fact]
        public void ValidateReport_WithMissingSections_ReturnsError()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.Sections.Clear();

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains("At least one section is required", errors);
        }

        [Fact]
        public void ValidateReport_WithInvalidSectionCode_ReturnsError()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.Sections[0].Code = "INVALID";

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains(errors, e => e.Contains("Invalid section code"));
        }

        [Fact]
        public void ValidateXml_WithValidXml_ReturnsNoErrors()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            var xml = _generator.GenerateTA7Report(report);

            // Act
            var errors = _validator.ValidateXml(xml);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateXml_WithInvalidXml_ReturnsError()
        {
            // Arrange
            var invalidXml = "<invalid>xml</invalid>";

            // Act
            var errors = _validator.ValidateXml(invalidXml);

            // Assert
            Assert.Contains(errors, e => e.Contains("Root element must be Bundle"));
        }

        [Fact]
        public void ValidateXml_WithMissingRequiredElements_ReturnsErrors()
        {
            // Arrange
            var incompleteXml = """
                <Bundle xmlns="http://hl7.org/fhir">
                    <id value="test"/>
                </Bundle>
                """;

            // Act
            var errors = _validator.ValidateXml(incompleteXml);

            // Assert
            Assert.Contains(errors, e => e.Contains("Required element 'meta' is missing"));
            Assert.Contains(errors, e => e.Contains("Required element 'identifier' is missing"));
            Assert.Contains(errors, e => e.Contains("Required element 'type' is missing"));
            Assert.Contains(errors, e => e.Contains("Required element 'timestamp' is missing"));
        }

        [Theory]
        [InlineData("123456789", true)]  // Valid IK number
        [InlineData("12345678", false)]  // Too short
        [InlineData("1234567890", false)] // Too long
        [InlineData("12345678a", false)] // Contains letter
        [InlineData("", false)]          // Empty
        public void ValidateReport_IKNumberValidation_WorksCorrectly(string ikNumber, bool shouldBeValid)
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.IKEmpfaenger = ikNumber;

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            if (shouldBeValid)
            {
                Assert.DoesNotContain(errors, e => e.Contains("Invalid IK Empfänger format"));
            }
            else
            {
                Assert.Contains(errors, e => e.Contains("IK Empfänger") && (e.Contains("format") || e.Contains("required")));
            }
        }

        [Theory]
        [InlineData("160.000.200.744.243.73", true)]  // Valid prescription ID
        [InlineData("160-000-200-744-243-73", false)] // Wrong separator
        [InlineData("160.000.200.744.243", false)]    // Too short
        [InlineData("160.000.200.744.243.734", false)] // Too long
        [InlineData("", false)]                        // Empty
        public void ValidateReport_PrescriptionIdValidation_WorksCorrectly(string prescriptionId, bool shouldBeValid)
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            var invoiceEntry = report.Entries.FirstOrDefault(e => e.Resource.Type == "Invoice");
            if (invoiceEntry?.Resource.Properties.TryGetValue("invoice", out var invoiceObj) == true &&
                invoiceObj is TA7ReportModels.TA7Invoice invoice)
            {
                invoice.PrescriptionId = prescriptionId;
            }

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            if (shouldBeValid)
            {
                Assert.DoesNotContain(errors, e => e.Contains("Invalid prescription ID format"));
            }
            else
            {
                Assert.Contains(errors, e => e.Contains("prescription ID") && (e.Contains("format") || e.Contains("required")));
            }
        }

        [Fact]
        public void GetValidationSummary_WithNoErrors_ReturnsSuccessMessage()
        {
            // Arrange
            var errors = new List<string>();

            // Act
            var summary = _validator.GetValidationSummary(errors);

            // Assert
            Assert.Contains("validation passed", summary);
            Assert.Contains("No errors found", summary);
        }

        [Fact]
        public void GetValidationSummary_WithErrors_ReturnsFormattedSummary()
        {
            // Arrange
            var errors = new List<string>
            {
                "Required field is missing",
                "Invalid format detected",
                "Business rule violation"
            };

            // Act
            var summary = _validator.GetValidationSummary(errors);

            // Assert
            Assert.Contains("validation failed", summary);
            Assert.Contains("3 error(s) found", summary);
            Assert.Contains("Required field is missing", summary);
            Assert.Contains("Invalid format detected", summary);
            Assert.Contains("Business rule violation", summary);
        }

        [Fact]
        public void ValidateReport_WithFutureDates_ReturnsErrors()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.InvoiceDate = DateTime.Now.AddDays(10);
            report.Composition.Rechnungsdatum = DateTime.Now.AddDays(5);

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains(errors, e => e.Contains("Invoice date cannot be after Rechnungsdatum"));
        }

        [Fact]
        public void ValidateReport_WithCrossReferenceErrors_ReturnsErrors()
        {
            // Arrange
            var report = _generator.CreateSampleReport();
            report.Composition.Sections[0].EntryReference = "urn:uuid:non-existent-id";

            // Act
            var errors = _validator.ValidateReport(report);

            // Assert
            Assert.Contains(errors, e => e.Contains("references non-existent entry"));
        }
    }

    public class TA7ReportModelsTests
    {
        [Fact]
        public void TA7Report_DefaultValues_AreSetCorrectly()
        {
            // Act
            var report = new TA7ReportModels.TA7Report();

            // Assert
            Assert.NotNull(report.BundleId);
            Assert.Equal("1.3", report.ProfileVersion);
            Assert.NotNull(report.Identifier);
            Assert.NotNull(report.Composition);
            Assert.NotNull(report.Entries);
        }

        [Fact]
        public void TA7Identifier_DefaultValues_AreSetCorrectly()
        {
            // Act
            var identifier = new TA7ReportModels.TA7Identifier();

            // Assert
            Assert.Equal("https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Dateiname", identifier.SystemValue);
            Assert.NotNull(identifier.Value);
            Assert.NotNull(identifier.Dateinummer);
        }

        [Fact]
        public void TA7Invoice_DefaultValues_AreSetCorrectly()
        {
            // Act
            var invoice = new TA7ReportModels.TA7Invoice();

            // Assert
            Assert.NotNull(invoice.Id);
            Assert.Equal("issued", invoice.Status);
            Assert.NotNull(invoice.Issuer);
            Assert.NotNull(invoice.LineItems);
        }

        [Fact]
        public void TA7LineItem_DefaultValues_AreSetCorrectly()
        {
            // Act
            var lineItem = new TA7ReportModels.TA7LineItem();

            // Assert
            Assert.Equal("1", lineItem.Positionstyp);
            Assert.Equal("0", lineItem.Import);
            Assert.Equal(1, lineItem.Sequence);
            Assert.Equal("UNC", lineItem.ChargeItemCode);
            Assert.NotNull(lineItem.PriceComponents);
        }

        [Fact]
        public void TA7PriceComponent_DefaultValues_AreSetCorrectly()
        {
            // Act
            var priceComponent = new TA7ReportModels.TA7PriceComponent();

            // Assert
            Assert.Equal("deduction", priceComponent.Type);
            Assert.Equal("EUR", priceComponent.Currency);
            Assert.Equal(0, priceComponent.Amount);
        }
    }
}
