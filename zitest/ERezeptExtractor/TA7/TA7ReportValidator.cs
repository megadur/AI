using System.Xml.Linq;
using System.Xml.Schema;
using System.Text.RegularExpressions;
using System.Text;

namespace ERezeptExtractor.TA7
{
    /// <summary>
    /// Validation service for TA7 FHIR reports
    /// Ensures compliance with GKVSV specifications for German health insurance billing
    /// </summary>
    public class TA7ReportValidator
    {
        private readonly Dictionary<string, Regex> _validationPatterns = new();
        private readonly HashSet<string> _validProfileVersions = new();
        private readonly HashSet<string> _validRechnungsarten = new();
        private readonly HashSet<string> _validZuAbschlagKeys = new();

        public TA7ReportValidator()
        {
            InitializeValidationPatterns();
            InitializeValidValues();
        }

        /// <summary>
        /// Validate a TA7 report model
        /// </summary>
        /// <param name="report">The TA7 report to validate</param>
        /// <returns>List of validation errors</returns>
        public List<string> ValidateReport(TA7ReportModels.TA7Report report)
        {
            var errors = new List<string>();

            try
            {
                // Basic report validation
                ValidateBasicReport(report, errors);

                // Identifier validation
                ValidateIdentifier(report.Identifier, errors);

                // Composition validation
                ValidateComposition(report.Composition, errors);

                // Entries validation
                ValidateEntries(report.Entries, errors);

                // Cross-reference validation
                ValidateCrossReferences(report, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Validate a generated TA7 XML against structure requirements
        /// </summary>
        /// <param name="xmlContent">The XML content to validate</param>
        /// <returns>List of validation errors</returns>
        public List<string> ValidateXml(string xmlContent)
        {
            var errors = new List<string>();

            try
            {
                // Parse XML
                var document = XDocument.Parse(xmlContent);
                var bundle = document.Root;

                if (bundle?.Name.LocalName != "Bundle")
                {
                    errors.Add("Root element must be Bundle");
                    return errors;
                }

                // Validate namespace
                if (bundle.Name.NamespaceName != "http://hl7.org/fhir")
                {
                    errors.Add("Bundle must use FHIR namespace");
                }

                // Validate required elements
                ValidateRequiredXmlElements(bundle, errors);

                // Validate profile references
                ValidateProfileReferences(bundle, errors);

                // Validate identifiers
                ValidateXmlIdentifiers(bundle, errors);

                // Validate business rules
                ValidateBusinessRules(bundle, errors);
            }
            catch (XmlSchemaException ex)
            {
                errors.Add($"XML Schema validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"XML validation error: {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Initialize validation patterns
        /// </summary>
        private void InitializeValidationPatterns()
        {
            _validationPatterns.Add("IK_Number", new Regex(@"^\d{9}$", RegexOptions.Compiled));
            _validationPatterns.Add("Prescription_ID", new Regex(@"^\d{3}\.\d{3}\.\d{3}\.\d{3}\.\d{3}\.\d{2}$", RegexOptions.Compiled));
            _validationPatterns.Add("Rechnungsnummer", new Regex(@"^\d{9}-\d{2}-\d{4}$", RegexOptions.Compiled));
            _validationPatterns.Add("Belegnummer", new Regex(@"^\d{19}$", RegexOptions.Compiled));
            _validationPatterns.Add("Dateinummer", new Regex(@"^\d{5}$", RegexOptions.Compiled));
            _validationPatterns.Add("Filename", new Regex(@"^[A-Z]{6}\d{5}$", RegexOptions.Compiled));
            _validationPatterns.Add("GUID", new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled));
        }

        /// <summary>
        /// Initialize valid values
        /// </summary>
        private void InitializeValidValues()
        {
            _validProfileVersions.UnionWith(new[] { "1.3", "1.2", "1.1", "1.0" });
            _validRechnungsarten.UnionWith(new[] { "1", "2", "3", "4" });
            _validZuAbschlagKeys.UnionWith(new[] 
            { 
                "R001", "R002", "R003", "R004", "R005", "R006", "R007", "R008", "R009", "R010",
                "Z001", "Z002", "Z003", "Z004", "Z005"
            });
        }

        /// <summary>
        /// Validate basic report structure
        /// </summary>
        private void ValidateBasicReport(TA7ReportModels.TA7Report report, List<string> errors)
        {
            if (report == null)
            {
                errors.Add("Report cannot be null");
                return;
            }

            if (string.IsNullOrEmpty(report.BundleId))
            {
                errors.Add("Bundle ID is required");
            }
            else if (!_validationPatterns["GUID"].IsMatch(report.BundleId))
            {
                errors.Add("Bundle ID must be a valid GUID");
            }

            if (string.IsNullOrEmpty(report.ProfileVersion))
            {
                errors.Add("Profile version is required");
            }
            else if (!_validProfileVersions.Contains(report.ProfileVersion))
            {
                errors.Add($"Invalid profile version: {report.ProfileVersion}");
            }

            if (report.Timestamp == default)
            {
                errors.Add("Timestamp is required");
            }
            else if (report.Timestamp > DateTime.Now.AddDays(1))
            {
                errors.Add("Timestamp cannot be in the future");
            }
        }

        /// <summary>
        /// Validate identifier
        /// </summary>
        private void ValidateIdentifier(TA7ReportModels.TA7Identifier identifier, List<string> errors)
        {
            if (identifier == null)
            {
                errors.Add("Identifier is required");
                return;
            }

            if (string.IsNullOrEmpty(identifier.Value))
            {
                errors.Add("Identifier value is required");
            }
            else if (!_validationPatterns["Filename"].IsMatch(identifier.Value))
            {
                errors.Add($"Invalid filename format: {identifier.Value}");
            }

            if (!string.IsNullOrEmpty(identifier.Dateinummer) && 
                !_validationPatterns["Dateinummer"].IsMatch(identifier.Dateinummer))
            {
                errors.Add($"Invalid Dateinummer format: {identifier.Dateinummer}");
            }

            if (string.IsNullOrEmpty(identifier.SystemValue))
            {
                errors.Add("Identifier system value is required");
            }
        }

        /// <summary>
        /// Validate composition
        /// </summary>
        private void ValidateComposition(TA7ReportModels.TA7Composition composition, List<string> errors)
        {
            if (composition == null)
            {
                errors.Add("Composition is required");
                return;
            }

            // Validate IK numbers
            ValidateIKNumber(composition.IKEmpfaenger, "IK Empfänger", errors);
            ValidateIKNumber(composition.IKKostentraeger, "IK Kostenträger", errors);
            ValidateIKNumber(composition.AuthorIK, "Author IK", errors);

            // Validate invoice number
            if (string.IsNullOrEmpty(composition.Rechnungsnummer))
            {
                errors.Add("Rechnungsnummer is required");
            }
            else if (!_validationPatterns["Rechnungsnummer"].IsMatch(composition.Rechnungsnummer))
            {
                errors.Add($"Invalid Rechnungsnummer format: {composition.Rechnungsnummer}");
            }

            // Validate dates
            if (composition.InvoiceDate == default)
            {
                errors.Add("Invoice date is required");
            }

            if (composition.Rechnungsdatum == default)
            {
                errors.Add("Rechnungsdatum is required");
            }

            if (composition.InvoiceDate > composition.Rechnungsdatum)
            {
                errors.Add("Invoice date cannot be after Rechnungsdatum");
            }

            // Validate sections
            if (composition.Sections == null || !composition.Sections.Any())
            {
                errors.Add("At least one section is required");
            }
            else
            {
                ValidateSections(composition.Sections, errors);
            }
        }

        /// <summary>
        /// Validate IK number
        /// </summary>
        private void ValidateIKNumber(string ikNumber, string fieldName, List<string> errors)
        {
            if (string.IsNullOrEmpty(ikNumber))
            {
                errors.Add($"{fieldName} is required");
            }
            else if (!_validationPatterns["IK_Number"].IsMatch(ikNumber))
            {
                errors.Add($"Invalid {fieldName} format: {ikNumber}");
            }
        }

        /// <summary>
        /// Validate sections
        /// </summary>
        private void ValidateSections(List<TA7ReportModels.TA7Section> sections, List<string> errors)
        {
            var validCodes = new HashSet<string> { "LR", "RB" };
            var foundCodes = new HashSet<string>();

            foreach (var section in sections)
            {
                if (string.IsNullOrEmpty(section.Code))
                {
                    errors.Add("Section code is required");
                }
                else if (!validCodes.Contains(section.Code))
                {
                    errors.Add($"Invalid section code: {section.Code}");
                }
                else if (!foundCodes.Add(section.Code))
                {
                    errors.Add($"Duplicate section code: {section.Code}");
                }

                if (string.IsNullOrEmpty(section.EntryReference))
                {
                    errors.Add("Section entry reference is required");
                }
                else if (!section.EntryReference.StartsWith("urn:uuid:"))
                {
                    errors.Add($"Invalid entry reference format: {section.EntryReference}");
                }
            }

            // Check required sections
            if (!foundCodes.Contains("LR"))
            {
                errors.Add("LR (List) section is required");
            }
            if (!foundCodes.Contains("RB"))
            {
                errors.Add("RB (Receipt Bundle) section is required");
            }
        }

        /// <summary>
        /// Validate entries
        /// </summary>
        private void ValidateEntries(List<TA7ReportModels.TA7Entry> entries, List<string> errors)
        {
            if (entries == null || !entries.Any())
            {
                errors.Add("At least one entry is required");
                return;
            }

            var foundResourceTypes = new HashSet<string>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.FullUrl))
                {
                    errors.Add("Entry fullUrl is required");
                }
                else if (!entry.FullUrl.StartsWith("urn:uuid:"))
                {
                    errors.Add($"Invalid fullUrl format: {entry.FullUrl}");
                }

                if (entry.Resource == null)
                {
                    errors.Add("Entry resource is required");
                    continue;
                }

                foundResourceTypes.Add(entry.Resource.Type);
                ValidateResource(entry.Resource, errors);
            }

            // Check required resource types
            var requiredTypes = new[] { "List", "Bundle" };
            foreach (var requiredType in requiredTypes)
            {
                if (!foundResourceTypes.Contains(requiredType))
                {
                    errors.Add($"{requiredType} resource is required");
                }
            }
        }

        /// <summary>
        /// Validate resource
        /// </summary>
        private void ValidateResource(TA7ReportModels.TA7Resource resource, List<string> errors)
        {
            if (string.IsNullOrEmpty(resource.Type))
            {
                errors.Add("Resource type is required");
            }

            if (string.IsNullOrEmpty(resource.Id))
            {
                errors.Add("Resource ID is required");
            }
            else if (!_validationPatterns["GUID"].IsMatch(resource.Id))
            {
                errors.Add($"Invalid resource ID format: {resource.Id}");
            }

            if (string.IsNullOrEmpty(resource.Profile))
            {
                errors.Add("Resource profile is required");
            }

            // Type-specific validation
            switch (resource.Type)
            {
                case "Invoice":
                    ValidateInvoiceResource(resource, errors);
                    break;
                case "Binary":
                    ValidateBinaryResource(resource, errors);
                    break;
            }
        }

        /// <summary>
        /// Validate invoice resource
        /// </summary>
        private void ValidateInvoiceResource(TA7ReportModels.TA7Resource resource, List<string> errors)
        {
            if (!resource.Properties.TryGetValue("invoice", out var invoiceObj) ||
                invoiceObj is not TA7ReportModels.TA7Invoice invoice)
            {
                errors.Add("Invoice resource must contain invoice data");
                return;
            }

            if (string.IsNullOrEmpty(invoice.PrescriptionId))
            {
                errors.Add("Prescription ID is required");
            }
            else if (!_validationPatterns["Prescription_ID"].IsMatch(invoice.PrescriptionId))
            {
                errors.Add($"Invalid prescription ID format: {invoice.PrescriptionId}");
            }

            if (string.IsNullOrEmpty(invoice.Belegnummer))
            {
                errors.Add("Belegnummer is required");
            }
            else if (!_validationPatterns["Belegnummer"].IsMatch(invoice.Belegnummer))
            {
                errors.Add($"Invalid Belegnummer format: {invoice.Belegnummer}");
            }

            ValidateInvoiceIssuer(invoice.Issuer, errors);
            ValidateLineItems(invoice.LineItems, errors);
        }

        /// <summary>
        /// Validate invoice issuer
        /// </summary>
        private void ValidateInvoiceIssuer(TA7ReportModels.TA7Issuer issuer, List<string> errors)
        {
            if (issuer == null)
            {
                errors.Add("Invoice issuer is required");
                return;
            }

            ValidateIKNumber(issuer.IKNumber, "Issuer IK", errors);

            if (string.IsNullOrEmpty(issuer.Sitz))
            {
                errors.Add("Issuer Sitz is required");
            }

            if (string.IsNullOrEmpty(issuer.LeistungserbringerTyp))
            {
                errors.Add("Issuer LeistungserbringerTyp is required");
            }
        }

        /// <summary>
        /// Validate line items
        /// </summary>
        private void ValidateLineItems(List<TA7ReportModels.TA7LineItem> lineItems, List<string> errors)
        {
            if (lineItems == null || !lineItems.Any())
            {
                errors.Add("At least one line item is required");
                return;
            }

            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                
                if (lineItem.Sequence != i + 1)
                {
                    errors.Add($"Line item {i + 1} has incorrect sequence number: {lineItem.Sequence}");
                }

                if (lineItem.VatValue < 0)
                {
                    errors.Add($"Line item {i + 1} VAT value cannot be negative");
                }

                ValidatePriceComponents(lineItem.PriceComponents, i + 1, errors);
            }
        }

        /// <summary>
        /// Validate price components
        /// </summary>
        private void ValidatePriceComponents(List<TA7ReportModels.TA7PriceComponent> priceComponents, int lineItemNumber, List<string> errors)
        {
            if (priceComponents == null || !priceComponents.Any())
            {
                return; // Price components are optional
            }

            var usedCodes = new HashSet<string>();

            foreach (var priceComponent in priceComponents)
            {
                if (string.IsNullOrEmpty(priceComponent.Code))
                {
                    errors.Add($"Line item {lineItemNumber}: Price component code is required");
                }
                else if (!_validZuAbschlagKeys.Contains(priceComponent.Code))
                {
                    errors.Add($"Line item {lineItemNumber}: Invalid ZuAbschlag key: {priceComponent.Code}");
                }
                else if (!usedCodes.Add(priceComponent.Code))
                {
                    errors.Add($"Line item {lineItemNumber}: Duplicate ZuAbschlag key: {priceComponent.Code}");
                }

                if (string.IsNullOrEmpty(priceComponent.Currency))
                {
                    errors.Add($"Line item {lineItemNumber}: Price component currency is required");
                }
                else if (priceComponent.Currency != "EUR")
                {
                    errors.Add($"Line item {lineItemNumber}: Only EUR currency is supported");
                }
            }
        }

        /// <summary>
        /// Validate binary resource
        /// </summary>
        private void ValidateBinaryResource(TA7ReportModels.TA7Resource resource, List<string> errors)
        {
            if (!resource.Properties.TryGetValue("contentType", out var contentTypeObj))
            {
                errors.Add("Binary resource must have contentType");
            }
            else if (contentTypeObj.ToString() != "application/pkcs7-mime")
            {
                errors.Add("Binary resource contentType must be application/pkcs7-mime");
            }

            if (!resource.Properties.TryGetValue("data", out var dataObj) || string.IsNullOrEmpty(dataObj.ToString()))
            {
                errors.Add("Binary resource must have data");
            }
        }

        /// <summary>
        /// Validate cross-references between different parts of the report
        /// </summary>
        private void ValidateCrossReferences(TA7ReportModels.TA7Report report, List<string> errors)
        {
            // Check that section references match entry fullUrls
            var entryUrls = new HashSet<string>(report.Entries.Select(e => e.FullUrl));

            foreach (var section in report.Composition.Sections)
            {
                if (!entryUrls.Contains(section.EntryReference))
                {
                    errors.Add($"Section {section.Code} references non-existent entry: {section.EntryReference}");
                }
            }

            // Validate that referenced UUIDs in composition match actual entry IDs
            foreach (var entry in report.Entries)
            {
                var expectedFullUrl = $"urn:uuid:{entry.Resource.Id}";
                if (entry.FullUrl != expectedFullUrl)
                {
                    errors.Add($"Entry fullUrl {entry.FullUrl} does not match resource ID {entry.Resource.Id}");
                }
            }
        }

        /// <summary>
        /// Validate required XML elements
        /// </summary>
        private void ValidateRequiredXmlElements(XElement bundle, List<string> errors)
        {
            var requiredElements = new[] { "id", "meta", "identifier", "type", "timestamp" };

            foreach (var elementName in requiredElements)
            {
                if (bundle.Element(elementName) == null)
                {
                    errors.Add($"Required element '{elementName}' is missing from Bundle");
                }
            }

            // Check for entry elements
            if (!bundle.Elements("entry").Any())
            {
                errors.Add("Bundle must contain at least one entry");
            }
        }

        /// <summary>
        /// Validate profile references in XML
        /// </summary>
        private void ValidateProfileReferences(XElement bundle, List<string> errors)
        {
            // Validate bundle profile
            var bundleMeta = bundle.Element("meta");
            var bundleProfile = bundleMeta?.Element("profile");
            if (bundleProfile == null)
            {
                errors.Add("Bundle meta must contain profile reference");
            }
            else
            {
                var profileValue = bundleProfile.Attribute("value")?.Value;
                if (!profileValue?.Contains("GKVSV_PR_TA7_Rechnung_Bundle") == true)
                {
                    errors.Add("Bundle must use TA7 Rechnung Bundle profile");
                }
            }

            // Validate composition profile
            var compositionEntry = bundle.Elements("entry")
                .FirstOrDefault(e => e.Element("resource")?.Element("Composition") != null);
            
            if (compositionEntry != null)
            {
                var compositionMeta = compositionEntry.Element("resource")?.Element("Composition")?.Element("meta");
                var compositionProfile = compositionMeta?.Element("profile");
                if (compositionProfile == null)
                {
                    errors.Add("Composition meta must contain profile reference");
                }
                else
                {
                    var profileValue = compositionProfile.Attribute("value")?.Value;
                    if (!profileValue?.Contains("GKVSV_PR_TA7_Rechnung_Composition") == true)
                    {
                        errors.Add("Composition must use TA7 Rechnung Composition profile");
                    }
                }
            }
        }

        /// <summary>
        /// Validate XML identifiers
        /// </summary>
        private void ValidateXmlIdentifiers(XElement bundle, List<string> errors)
        {
            // Validate bundle identifier
            var bundleIdentifier = bundle.Element("identifier");
            if (bundleIdentifier != null)
            {
                var systemValue = bundleIdentifier.Element("system")?.Attribute("value")?.Value;
                var value = bundleIdentifier.Element("value")?.Attribute("value")?.Value;

                if (systemValue != "https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Dateiname")
                {
                    errors.Add("Bundle identifier system must be GKVSV_NS_Dateiname");
                }

                if (!string.IsNullOrEmpty(value) && !_validationPatterns["Filename"].IsMatch(value))
                {
                    errors.Add($"Invalid bundle identifier value format: {value}");
                }
            }
        }

        /// <summary>
        /// Validate business rules
        /// </summary>
        private void ValidateBusinessRules(XElement bundle, List<string> errors)
        {
            // Business rule: Invoice entries must have corresponding prescription data
            var invoiceEntries = bundle.Elements("entry")
                .Where(e => e.Element("resource")?.Element("Invoice") != null)
                .ToList();

            var bundleEntries = bundle.Elements("entry")
                .Where(e => e.Element("resource")?.Element("Bundle") != null)
                .ToList();

            if (invoiceEntries.Any() && !bundleEntries.Any())
            {
                errors.Add("Business rule violation: Invoice entries require corresponding prescription bundle entries");
            }

            // Business rule: All amounts in EUR
            var priceComponents = bundle.Descendants("priceComponent");
            foreach (var priceComponent in priceComponents)
            {
                var currency = priceComponent.Element("amount")?.Element("currency")?.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(currency) && currency != "EUR")
                {
                    errors.Add($"Business rule violation: All amounts must be in EUR, found: {currency}");
                }
            }

            // Business rule: Timestamp must be reasonable
            var timestamp = bundle.Element("timestamp")?.Attribute("value")?.Value;
            if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, out var parsedTimestamp))
            {
                if (parsedTimestamp > DateTime.Now.AddDays(1))
                {
                    errors.Add("Business rule violation: Timestamp cannot be more than 1 day in the future");
                }
                if (parsedTimestamp < DateTime.Now.AddYears(-1))
                {
                    errors.Add("Business rule violation: Timestamp cannot be more than 1 year in the past");
                }
            }
        }

        /// <summary>
        /// Get validation summary
        /// </summary>
        /// <param name="errors">List of validation errors</param>
        /// <returns>Validation summary</returns>
        public string GetValidationSummary(List<string> errors)
        {
            if (!errors.Any())
            {
                return "✓ TA7 report validation passed - No errors found";
            }

            var summary = new StringBuilder();
            summary.AppendLine($"✗ TA7 report validation failed - {errors.Count} error(s) found:");
            summary.AppendLine();

            var groupedErrors = errors.GroupBy(GetErrorCategory).ToList();

            foreach (var group in groupedErrors)
            {
                summary.AppendLine($"[{group.Key}]");
                foreach (var error in group)
                {
                    summary.AppendLine($"  - {error}");
                }
                summary.AppendLine();
            }

            return summary.ToString();
        }

        /// <summary>
        /// Get error category for grouping
        /// </summary>
        private string GetErrorCategory(string error)
        {
            if (error.Contains("required") || error.Contains("missing"))
                return "Required Fields";
            if (error.Contains("format") || error.Contains("Invalid"))
                return "Format Errors";
            if (error.Contains("Business rule"))
                return "Business Rules";
            if (error.Contains("Cross-reference") || error.Contains("references"))
                return "Reference Errors";
            if (error.Contains("XML") || error.Contains("element"))
                return "XML Structure";
            
            return "Other";
        }
    }
}
