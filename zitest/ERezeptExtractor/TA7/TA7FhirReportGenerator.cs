using System.Xml.Linq;
using System.Text;

namespace ERezeptExtractor.TA7
{
    /// <summary>
    /// Models for TA7 FHIR report generation (German electronic prescription billing)
    /// Based on GKVSV (Spitzenverband der gesetzlichen Krankenversicherung) specifications
    /// </summary>
    public class TA7ReportModels
    {
        public class TA7Report
        {
            public string BundleId { get; set; } = Guid.NewGuid().ToString();
            public string ProfileVersion { get; set; } = "1.3";
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public TA7Identifier Identifier { get; set; } = new();
            public TA7Composition Composition { get; set; } = new();
            public List<TA7Entry> Entries { get; set; } = new();
        }

        public class TA7Identifier
        {
            public string Dateinummer { get; set; } = string.Empty; // File number extension
            public string SystemValue { get; set; } = "https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Dateiname";
            public string Value { get; set; } = string.Empty; // e.g., "ARZFHR24036"
        }

        public class TA7Composition
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string IKEmpfaenger { get; set; } = string.Empty; // Receiver IK number
            public string IKKostentraeger { get; set; } = string.Empty; // Cost bearer IK number
            public string Rechnungsnummer { get; set; } = string.Empty; // Invoice number
            public DateTime InvoiceDate { get; set; } = DateTime.Now;
            public DateTime Rechnungsdatum { get; set; } = DateTime.Now; // Billing date
            public string AuthorIK { get; set; } = string.Empty; // Author IK number
            public List<TA7Section> Sections { get; set; } = new();
        }

        public class TA7Section
        {
            public string Code { get; set; } = string.Empty; // "LR" or "RB"
            public string EntryReference { get; set; } = string.Empty;
        }

        public class TA7Entry
        {
            public string FullUrl { get; set; } = string.Empty;
            public TA7Resource Resource { get; set; } = new();
        }

        public class TA7Resource
        {
            public string Type { get; set; } = string.Empty; // "List", "Bundle", "Binary", "Invoice"
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Profile { get; set; } = string.Empty;
            public Dictionary<string, object> Properties { get; set; } = new();
        }

        public class TA7RezeptBundle
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Type { get; set; } = "collection";
            public List<TA7RezeptEntry> Entries { get; set; } = new();
        }

        public class TA7RezeptEntry
        {
            public string Relation { get; set; } = "item";
            public string Url { get; set; } = string.Empty;
            public string FullUrl { get; set; } = string.Empty;
            public TA7Binary Resource { get; set; } = new();
        }

        public class TA7Binary
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string ContentType { get; set; } = "application/pkcs7-mime";
            public string Data { get; set; } = string.Empty; // Base64 encoded
        }

        public class TA7Invoice
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string PrescriptionId { get; set; } = string.Empty;
            public string Belegnummer { get; set; } = string.Empty; // Receipt number
            public string Status { get; set; } = "issued";
            public TA7Issuer Issuer { get; set; } = new();
            public List<TA7LineItem> LineItems { get; set; } = new();
        }

        public class TA7Issuer
        {
            public string Sitz { get; set; } = "1"; // Service provider seat
            public string LeistungserbringerTyp { get; set; } = "A"; // Service provider type
            public string IKNumber { get; set; } = string.Empty;
        }

        public class TA7LineItem
        {
            public string Positionstyp { get; set; } = "1";
            public string Import { get; set; } = "0";
            public decimal VatValue { get; set; } = 0;
            public int Sequence { get; set; } = 1;
            public string ChargeItemCode { get; set; } = "UNC";
            public List<TA7PriceComponent> PriceComponents { get; set; } = new();
        }

        public class TA7PriceComponent
        {
            public string Type { get; set; } = "deduction";
            public string Code { get; set; } = string.Empty; // e.g., "R001", "R004", etc.
            public decimal Amount { get; set; } = 0;
            public string Currency { get; set; } = "EUR";
        }
    }

    /// <summary>
    /// Generator for TA7 FHIR reports used in German electronic prescription billing
    /// Follows GKVSV specifications for health insurance billing
    /// </summary>
    public class TA7FhirReportGenerator
    {
        private readonly string _fhirNamespace = "http://hl7.org/fhir";
        private readonly string _gkvsv_profile_base = "https://fhir.gkvsv.de/StructureDefinition/";

        /// <summary>
        /// Generate a complete TA7 FHIR report XML
        /// </summary>
        /// <param name="report">TA7 report data</param>
        /// <returns>Generated XML as string</returns>
        public string GenerateTA7Report(TA7ReportModels.TA7Report report)
        {
            try
            {
                var bundle = CreateBundle(report);
                
                // Format XML with proper indentation
                var stringBuilder = new StringBuilder();
                using (var writer = System.Xml.XmlWriter.Create(stringBuilder, new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false
                }))
                {
                    bundle.WriteTo(writer);
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate TA7 FHIR report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create the main Bundle element
        /// </summary>
        private XElement CreateBundle(TA7ReportModels.TA7Report report)
        {
            var bundle = new XElement(XName.Get("Bundle", _fhirNamespace),
                new XAttribute("xmlns", _fhirNamespace),
                new XElement("id", new XAttribute("value", report.BundleId)),
                CreateMeta(report.ProfileVersion),
                CreateIdentifier(report.Identifier),
                new XElement("type", new XAttribute("value", "collection")),
                new XElement("timestamp", new XAttribute("value", FormatDateTime(report.Timestamp)))
            );

            // Add composition entry
            bundle.Add(CreateCompositionEntry(report.Composition));

            // Add other entries
            foreach (var entry in report.Entries)
            {
                bundle.Add(CreateEntry(entry));
            }

            return bundle;
        }

        /// <summary>
        /// Create meta element with profile information
        /// </summary>
        private XElement CreateMeta(string profileVersion)
        {
            return new XElement("meta",
                new XElement("profile", 
                    new XAttribute("value", $"{_gkvsv_profile_base}GKVSV_PR_TA7_Rechnung_Bundle|{profileVersion}"))
            );
        }

        /// <summary>
        /// Create identifier with file number extension
        /// </summary>
        private XElement CreateIdentifier(TA7ReportModels.TA7Identifier identifier)
        {
            var identifierElement = new XElement("identifier",
                new XElement("system", new XAttribute("value", identifier.SystemValue)),
                new XElement("value", new XAttribute("value", identifier.Value))
            );

            if (!string.IsNullOrEmpty(identifier.Dateinummer))
            {
                identifierElement.AddFirst(new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_TA7_Dateinummer"),
                    new XElement("valueString", new XAttribute("value", identifier.Dateinummer))
                ));
            }

            return identifierElement;
        }

        /// <summary>
        /// Create composition entry for the report
        /// </summary>
        private XElement CreateCompositionEntry(TA7ReportModels.TA7Composition composition)
        {
            var compositionResource = new XElement("Composition",
                new XElement("id", new XAttribute("value", composition.Id)),
                CreateMeta($"{_gkvsv_profile_base}GKVSV_PR_TA7_Rechnung_Composition|1.3"),
                CreateCompositionExtensions(composition),
                CreateCompositionIdentifier(composition.Rechnungsnummer),
                new XElement("status", new XAttribute("value", "final")),
                CreateCompositionType(),
                CreateCompositionDate(composition),
                CreateCompositionAuthor(composition.AuthorIK),
                new XElement("title", new XAttribute("value", "elektronische Rechnung"))
            );

            // Add sections
            foreach (var section in composition.Sections)
            {
                compositionResource.Add(CreateSection(section));
            }

            return new XElement("entry",
                new XElement("fullUrl", new XAttribute("value", $"urn:uuid:{composition.Id}")),
                new XElement("resource", compositionResource)
            );
        }

        /// <summary>
        /// Create composition extensions for IK numbers
        /// </summary>
        private XElement[] CreateCompositionExtensions(TA7ReportModels.TA7Composition composition)
        {
            return new[]
            {
                new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_TA7_IK_Empfaenger"),
                    new XElement("valueIdentifier",
                        new XElement("system", new XAttribute("value", "http://fhir.de/sid/arge-ik/iknr")),
                        new XElement("value", new XAttribute("value", composition.IKEmpfaenger))
                    )
                ),
                new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_TA7_IK_Kostentraeger"),
                    new XElement("valueIdentifier",
                        new XElement("system", new XAttribute("value", "http://fhir.de/sid/arge-ik/iknr")),
                        new XElement("value", new XAttribute("value", composition.IKKostentraeger))
                    )
                )
            };
        }

        /// <summary>
        /// Create composition identifier
        /// </summary>
        private XElement CreateCompositionIdentifier(string rechnungsnummer)
        {
            return new XElement("identifier",
                new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Rechnungsnummer")),
                new XElement("value", new XAttribute("value", rechnungsnummer))
            );
        }

        /// <summary>
        /// Create composition type
        /// </summary>
        private XElement CreateCompositionType()
        {
            return new XElement("type",
                new XElement("coding",
                    new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_Rechnungsart")),
                    new XElement("code", new XAttribute("value", "3"))
                )
            );
        }

        /// <summary>
        /// Create composition date with extension
        /// </summary>
        private XElement CreateCompositionDate(TA7ReportModels.TA7Composition composition)
        {
            var dateElement = new XElement("date", new XAttribute("value", FormatDate(composition.InvoiceDate)));
            
            dateElement.Add(new XElement("extension",
                new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_TA7_Rechnungsdatum"),
                new XElement("valueDateTime", new XAttribute("value", FormatDate(composition.Rechnungsdatum)))
            ));

            return dateElement;
        }

        /// <summary>
        /// Create composition author
        /// </summary>
        private XElement CreateCompositionAuthor(string authorIK)
        {
            return new XElement("author",
                new XElement("identifier",
                    new XElement("system", new XAttribute("value", "http://fhir.de/sid/arge-ik/iknr")),
                    new XElement("value", new XAttribute("value", authorIK))
                )
            );
        }

        /// <summary>
        /// Create section element
        /// </summary>
        private XElement CreateSection(TA7ReportModels.TA7Section section)
        {
            return new XElement("section",
                new XElement("code",
                    new XElement("coding",
                        new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_TA7")),
                        new XElement("code", new XAttribute("value", section.Code))
                    )
                ),
                new XElement("entry",
                    new XElement("reference", new XAttribute("value", section.EntryReference))
                )
            );
        }

        /// <summary>
        /// Create generic entry
        /// </summary>
        private XElement CreateEntry(TA7ReportModels.TA7Entry entry)
        {
            var entryElement = new XElement("entry",
                new XElement("fullUrl", new XAttribute("value", entry.FullUrl))
            );

            // Create resource based on type
            var resource = CreateResourceByType(entry.Resource);
            entryElement.Add(new XElement("resource", resource));

            return entryElement;
        }

        /// <summary>
        /// Create resource based on its type
        /// </summary>
        private XElement CreateResourceByType(TA7ReportModels.TA7Resource resource)
        {
            return resource.Type switch
            {
                "List" => CreateListResource(resource),
                "Bundle" => CreateBundleResource(resource),
                "Binary" => CreateBinaryResource(resource),
                "Invoice" => CreateInvoiceResource(resource),
                _ => throw new NotSupportedException($"Resource type '{resource.Type}' is not supported")
            };
        }

        /// <summary>
        /// Create List resource
        /// </summary>
        private XElement CreateListResource(TA7ReportModels.TA7Resource resource)
        {
            return new XElement("List",
                new XElement("id", new XAttribute("value", resource.Id)),
                CreateMeta(resource.Profile),
                new XElement("status", new XAttribute("value", "current")),
                new XElement("mode", new XAttribute("value", "working")),
                CreateListEntry(resource.Properties)
            );
        }

        /// <summary>
        /// Create list entry
        /// </summary>
        private XElement CreateListEntry(Dictionary<string, object> properties)
        {
            var fileName = properties.GetValueOrDefault("fileName", "").ToString();
            
            return new XElement("entry",
                new XElement("item",
                    new XElement("identifier",
                        new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Dateiname")),
                        new XElement("value", new XAttribute("value", fileName))
                    )
                )
            );
        }

        /// <summary>
        /// Create Bundle resource for prescription data
        /// </summary>
        private XElement CreateBundleResource(TA7ReportModels.TA7Resource resource)
        {
            var bundleElement = new XElement("Bundle",
                new XElement("id", new XAttribute("value", resource.Id)),
                CreateMeta(resource.Profile),
                new XElement("type", new XAttribute("value", "collection"))
            );

            // Add entries if provided
            if (resource.Properties.TryGetValue("entries", out var entriesObj) && 
                entriesObj is List<TA7ReportModels.TA7RezeptEntry> entries)
            {
                foreach (var entry in entries)
                {
                    bundleElement.Add(CreateRezeptEntry(entry));
                }
            }

            return bundleElement;
        }

        /// <summary>
        /// Create prescription entry with link
        /// </summary>
        private XElement CreateRezeptEntry(TA7ReportModels.TA7RezeptEntry entry)
        {
            return new XElement("entry",
                new XElement("link",
                    new XElement("relation", new XAttribute("value", entry.Relation)),
                    new XElement("url", new XAttribute("value", entry.Url))
                ),
                new XElement("fullUrl", new XAttribute("value", entry.FullUrl)),
                new XElement("resource",
                    CreateBinaryResource(new TA7ReportModels.TA7Resource
                    {
                        Type = "Binary",
                        Id = entry.Resource.Id,
                        Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_Binary|1.3",
                        Properties = new Dictionary<string, object>
                        {
                            ["contentType"] = entry.Resource.ContentType,
                            ["data"] = entry.Resource.Data
                        }
                    })
                )
            );
        }

        /// <summary>
        /// Create Binary resource
        /// </summary>
        private XElement CreateBinaryResource(TA7ReportModels.TA7Resource resource)
        {
            var contentType = resource.Properties.GetValueOrDefault("contentType", "application/pkcs7-mime").ToString();
            var data = resource.Properties.GetValueOrDefault("data", "").ToString();

            return new XElement("Binary",
                new XElement("id", new XAttribute("value", resource.Id)),
                CreateMeta(resource.Profile),
                new XElement("contentType", new XAttribute("value", contentType)),
                new XElement("data", new XAttribute("value", data))
            );
        }

        /// <summary>
        /// Create Invoice resource
        /// </summary>
        private XElement CreateInvoiceResource(TA7ReportModels.TA7Resource resource)
        {
            if (!resource.Properties.TryGetValue("invoice", out var invoiceObj) || 
                invoiceObj is not TA7ReportModels.TA7Invoice invoice)
            {
                throw new ArgumentException("Invoice data is required for Invoice resource");
            }

            var invoiceElement = new XElement("Invoice",
                new XElement("id", new XAttribute("value", resource.Id)),
                CreateMeta(resource.Profile),
                CreateInvoiceExtensions(),
                CreateInvoiceIdentifiers(invoice),
                new XElement("status", new XAttribute("value", invoice.Status)),
                CreateInvoiceIssuer(invoice.Issuer)
            );

            // Add line items
            foreach (var lineItem in invoice.LineItems)
            {
                invoiceElement.Add(CreateInvoiceLineItem(lineItem));
            }

            return invoiceElement;
        }

        /// <summary>
        /// Create invoice extensions
        /// </summary>
        private XElement CreateInvoiceExtensions()
        {
            return new XElement("extension",
                new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_Irrlaeufer"),
                new XElement("valueBoolean", new XAttribute("value", "false"))
            );
        }

        /// <summary>
        /// Create invoice identifiers
        /// </summary>
        private XElement[] CreateInvoiceIdentifiers(TA7ReportModels.TA7Invoice invoice)
        {
            return new[]
            {
                new XElement("identifier",
                    new XElement("system", new XAttribute("value", "https://gematik.de/fhir/erp/NamingSystem/GEM_ERP_NS_PrescriptionId")),
                    new XElement("value", new XAttribute("value", invoice.PrescriptionId))
                ),
                new XElement("identifier",
                    new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/NamingSystem/GKVSV_NS_Belegnummer")),
                    new XElement("value", new XAttribute("value", invoice.Belegnummer))
                )
            };
        }

        /// <summary>
        /// Create invoice issuer
        /// </summary>
        private XElement CreateInvoiceIssuer(TA7ReportModels.TA7Issuer issuer)
        {
            return new XElement("issuer",
                new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_LE_Sitz"),
                    new XElement("valueCoding",
                        new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_Leistungserbringer_Sitz")),
                        new XElement("code", new XAttribute("value", issuer.Sitz))
                    )
                ),
                new XElement("identifier",
                    new XElement("type",
                        new XElement("coding",
                            new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_Leistungserbringertyp")),
                            new XElement("code", new XAttribute("value", issuer.LeistungserbringerTyp))
                        )
                    ),
                    new XElement("system", new XAttribute("value", "http://fhir.de/sid/arge-ik/iknr")),
                    new XElement("value", new XAttribute("value", issuer.IKNumber))
                )
            );
        }

        /// <summary>
        /// Create invoice line item
        /// </summary>
        private XElement CreateInvoiceLineItem(TA7ReportModels.TA7LineItem lineItem)
        {
            var lineItemElement = new XElement("lineItem",
                CreateLineItemExtensions(lineItem),
                new XElement("sequence", new XAttribute("value", lineItem.Sequence.ToString())),
                CreateLineItemChargeCode(lineItem.ChargeItemCode)
            );

            // Add price components
            foreach (var priceComponent in lineItem.PriceComponents)
            {
                lineItemElement.Add(CreatePriceComponent(priceComponent));
            }

            return lineItemElement;
        }

        /// <summary>
        /// Create line item extensions
        /// </summary>
        private XElement[] CreateLineItemExtensions(TA7ReportModels.TA7LineItem lineItem)
        {
            return new[]
            {
                new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_Positionstyp"),
                    new XElement("valueCodeableConcept",
                        new XElement("coding",
                            new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_Positionstyp")),
                            new XElement("code", new XAttribute("value", lineItem.Positionstyp))
                        )
                    )
                ),
                new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_Import"),
                    new XElement("valueCodeableConcept",
                        new XElement("coding",
                            new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_Import")),
                            new XElement("code", new XAttribute("value", lineItem.Import))
                        )
                    )
                ),
                new XElement("extension",
                    new XAttribute("url", $"{_gkvsv_profile_base}GKVSV_EX_ERP_VAT_VALUE"),
                    new XElement("valueMoney",
                        new XElement("value", new XAttribute("value", lineItem.VatValue.ToString("F2"))),
                        new XElement("currency", new XAttribute("value", "EUR"))
                    )
                )
            };
        }

        /// <summary>
        /// Create line item charge code
        /// </summary>
        private XElement CreateLineItemChargeCode(string chargeItemCode)
        {
            return new XElement("chargeItemCodeableConcept",
                new XElement("coding",
                    new XElement("system", new XAttribute("value", "http://terminology.hl7.org/CodeSystem/v3-NullFlavor")),
                    new XElement("code", new XAttribute("value", chargeItemCode))
                )
            );
        }

        /// <summary>
        /// Create price component
        /// </summary>
        private XElement CreatePriceComponent(TA7ReportModels.TA7PriceComponent priceComponent)
        {
            return new XElement("priceComponent",
                new XElement("type", new XAttribute("value", priceComponent.Type)),
                new XElement("code",
                    new XElement("coding",
                        new XElement("system", new XAttribute("value", "https://fhir.gkvsv.de/CodeSystem/GKVSV_CS_ERP_ZuAbschlagKey")),
                        new XElement("code", new XAttribute("value", priceComponent.Code))
                    )
                ),
                new XElement("amount",
                    new XElement("value", new XAttribute("value", priceComponent.Amount.ToString("F2"))),
                    new XElement("currency", new XAttribute("value", priceComponent.Currency))
                )
            );
        }

        /// <summary>
        /// Format DateTime for FHIR
        /// </summary>
        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
        }

        /// <summary>
        /// Format Date for FHIR
        /// </summary>
        private string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Create a sample TA7 report for testing purposes
        /// </summary>
        public TA7ReportModels.TA7Report CreateSampleReport()
        {
            var prescriptionId = "160.000.200.744.243.73";
            var invoiceNumber = "120890780-24-0310";
            var listId = Guid.NewGuid().ToString();
            var bundleId = Guid.NewGuid().ToString();
            var invoiceId = Guid.NewGuid().ToString();

            return new TA7ReportModels.TA7Report
            {
                Identifier = new TA7ReportModels.TA7Identifier
                {
                    Dateinummer = "01326",
                    Value = "ARZFHR24036"
                },
                Composition = new TA7ReportModels.TA7Composition
                {
                    IKEmpfaenger = "107436557",
                    IKKostentraeger = "120890780",
                    Rechnungsnummer = invoiceNumber,
                    InvoiceDate = DateTime.Parse("2024-03-31"),
                    Rechnungsdatum = DateTime.Parse("2024-04-05"),
                    AuthorIK = "309520770",
                    Sections = new List<TA7ReportModels.TA7Section>
                    {
                        new() { Code = "LR", EntryReference = $"urn:uuid:{listId}" },
                        new() { Code = "RB", EntryReference = $"urn:uuid:{bundleId}" }
                    }
                },
                Entries = new List<TA7ReportModels.TA7Entry>
                {
                    // List entry
                    new()
                    {
                        FullUrl = $"urn:uuid:{listId}",
                        Resource = new TA7ReportModels.TA7Resource
                        {
                            Type = "List",
                            Id = listId,
                            Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_TA7_Rechnung_List|1.3",
                            Properties = new Dictionary<string, object>
                            {
                                ["fileName"] = "ARZFHR24036"
                            }
                        }
                    },
                    // Bundle entry with prescription data
                    new()
                    {
                        FullUrl = $"urn:uuid:{bundleId}",
                        Resource = new TA7ReportModels.TA7Resource
                        {
                            Type = "Bundle",
                            Id = bundleId,
                            Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_TA7_RezeptBundle|1.3",
                            Properties = new Dictionary<string, object>
                            {
                                ["entries"] = new List<TA7ReportModels.TA7RezeptEntry>
                                {
                                    new()
                                    {
                                        Url = "https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Bundle",
                                        FullUrl = $"urn:uuid:{Guid.NewGuid()}",
                                        Resource = new TA7ReportModels.TA7Binary
                                        {
                                            Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("<KBV Bundle data would go here>"))
                                        }
                                    },
                                    new()
                                    {
                                        Url = "https://gematik.de/fhir/StructureDefinition/ErxReceipt",
                                        FullUrl = $"urn:uuid:{Guid.NewGuid()}",
                                        Resource = new TA7ReportModels.TA7Binary
                                        {
                                            Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("<Receipt data would go here>"))
                                        }
                                    }
                                }
                            }
                        }
                    },
                    // Invoice entry
                    new()
                    {
                        FullUrl = $"urn:uuid:{invoiceId}",
                        Resource = new TA7ReportModels.TA7Resource
                        {
                            Type = "Invoice",
                            Id = invoiceId,
                            Profile = "https://fhir.gkvsv.de/StructureDefinition/GKVSV_PR_ERP_eAbrechnungsdaten|1.3",
                            Properties = new Dictionary<string, object>
                            {
                                ["invoice"] = new TA7ReportModels.TA7Invoice
                                {
                                    PrescriptionId = prescriptionId,
                                    Belegnummer = "2403000136899520770",
                                    Issuer = new TA7ReportModels.TA7Issuer
                                    {
                                        IKNumber = "300604282"
                                    },
                                    LineItems = new List<TA7ReportModels.TA7LineItem>
                                    {
                                        new()
                                        {
                                            VatValue = 2.06m,
                                            PriceComponents = new List<TA7ReportModels.TA7PriceComponent>
                                            {
                                                new() { Code = "R001", Amount = -2.00m },
                                                new() { Code = "R004", Amount = 0.00m },
                                                new() { Code = "R005", Amount = 0.00m },
                                                new() { Code = "R006", Amount = 0.00m },
                                                new() { Code = "R009", Amount = 0.00m }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
