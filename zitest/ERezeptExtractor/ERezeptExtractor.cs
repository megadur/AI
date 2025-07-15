using System.Xml;
using System.Xml.XPath;
using ERezeptExtractor.Models;

namespace ERezeptExtractor
{
    /// <summary>
    /// Main class for extracting data from eRezept FHIR XML files
    /// </summary>
    public class ERezeptExtractor
    {
        private readonly XmlNamespaceManager _namespaceManager;
        
        public ERezeptExtractor()
        {
            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("fhir", "http://hl7.org/fhir");
        }

        /// <summary>
        /// Extracts data from an eRezept FHIR XML string
        /// </summary>
        /// <param name="xmlContent">The XML content as string</param>
        /// <returns>Extracted eRezept data</returns>
        public ERezeptData ExtractFromXml(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("XML content cannot be null or empty", nameof(xmlContent));

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            
            return ExtractFromXmlDocument(xmlDoc);
        }

        /// <summary>
        /// Extracts data from an eRezept FHIR XML file
        /// </summary>
        /// <param name="filePath">Path to the XML file</param>
        /// <returns>Extracted eRezept data</returns>
        public ERezeptData ExtractFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var xmlContent = File.ReadAllText(filePath);
            return ExtractFromXml(xmlContent);
        }

        /// <summary>
        /// Extracts data from an XmlDocument
        /// </summary>
        /// <param name="xmlDoc">The XML document</param>
        /// <returns>Extracted eRezept data</returns>
        public ERezeptData ExtractFromXmlDocument(XmlDocument xmlDoc)
        {
            var data = new ERezeptData();

            // Extract Bundle level information
            ExtractBundleInfo(xmlDoc, data);
            
            // Extract Pharmacy information
            ExtractPharmacyInfo(xmlDoc, data);
            
            // Extract Medication Dispense information
            ExtractMedicationDispenseInfo(xmlDoc, data);
            
            // Extract Invoice information
            ExtractInvoiceInfo(xmlDoc, data);

            return data;
        }

        private void ExtractBundleInfo(XmlDocument xmlDoc, ERezeptData data)
        {
            var bundleNode = xmlDoc.SelectSingleNode("//fhir:Bundle", _namespaceManager);
            if (bundleNode != null)
            {
                data.BundleId = GetAttributeValue(bundleNode, "fhir:id/@value");
                
                var identifierNode = bundleNode.SelectSingleNode("fhir:identifier", _namespaceManager);
                if (identifierNode != null)
                {
                    data.PrescriptionId = GetAttributeValue(identifierNode, "fhir:value/@value");
                }

                var timestampValue = GetAttributeValue(bundleNode, "fhir:timestamp/@value");
                if (DateTime.TryParse(timestampValue, out var timestamp))
                {
                    data.Timestamp = timestamp;
                }
            }
        }

        private void ExtractPharmacyInfo(XmlDocument xmlDoc, ERezeptData data)
        {
            var organizationNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Organization", _namespaceManager);
            if (organizationNode != null)
            {
                data.Pharmacy.Id = GetAttributeValue(organizationNode, "fhir:id/@value");
                data.Pharmacy.Name = GetAttributeValue(organizationNode, "fhir:name/@value");
                
                var identifierNode = organizationNode.SelectSingleNode("fhir:identifier", _namespaceManager);
                if (identifierNode != null)
                {
                    data.Pharmacy.IK_Number = GetAttributeValue(identifierNode, "fhir:value/@value");
                }

                // Extract address
                ExtractAddress(organizationNode, data.Pharmacy.Address);
            }
        }

        private void ExtractAddress(XmlNode parentNode, AddressInfo address)
        {
            var addressNode = parentNode.SelectSingleNode("fhir:address", _namespaceManager);
            if (addressNode != null)
            {
                address.City = GetAttributeValue(addressNode, "fhir:city/@value");
                address.PostalCode = GetAttributeValue(addressNode, "fhir:postalCode/@value");
                address.Country = GetAttributeValue(addressNode, "fhir:country/@value");

                var lineNode = addressNode.SelectSingleNode("fhir:line", _namespaceManager);
                if (lineNode != null)
                {
                    var streetExtension = lineNode.SelectSingleNode("fhir:extension[@url='http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-streetName']", _namespaceManager);
                    if (streetExtension != null)
                    {
                        address.Street = GetAttributeValue(streetExtension, "fhir:valueString/@value");
                    }

                    var houseNumberExtension = lineNode.SelectSingleNode("fhir:extension[@url='http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-houseNumber']", _namespaceManager);
                    if (houseNumberExtension != null)
                    {
                        address.HouseNumber = GetAttributeValue(houseNumberExtension, "fhir:valueString/@value");
                    }
                }
            }
        }

        private void ExtractMedicationDispenseInfo(XmlDocument xmlDoc, ERezeptData data)
        {
            var medicationDispenseNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:MedicationDispense", _namespaceManager);
            if (medicationDispenseNode != null)
            {
                data.MedicationDispense.Id = GetAttributeValue(medicationDispenseNode, "fhir:id/@value");
                data.MedicationDispense.Status = GetAttributeValue(medicationDispenseNode, "fhir:status/@value");

                var prescriptionNode = medicationDispenseNode.SelectSingleNode("fhir:authorizingPrescription/fhir:identifier", _namespaceManager);
                if (prescriptionNode != null)
                {
                    data.MedicationDispense.PrescriptionId = GetAttributeValue(prescriptionNode, "fhir:value/@value");
                }

                var typeNode = medicationDispenseNode.SelectSingleNode("fhir:type/fhir:coding", _namespaceManager);
                if (typeNode != null)
                {
                    data.MedicationDispense.Type = GetAttributeValue(typeNode, "fhir:code/@value");
                }

                var handedOverValue = GetAttributeValue(medicationDispenseNode, "fhir:whenHandedOver/@value");
                if (DateTime.TryParse(handedOverValue, out var handedOver))
                {
                    data.MedicationDispense.HandedOverDate = handedOver;
                }
            }
        }

        private void ExtractInvoiceInfo(XmlDocument xmlDoc, ERezeptData data)
        {
            var invoiceNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Invoice", _namespaceManager);
            if (invoiceNode != null)
            {
                data.Invoice.Id = GetAttributeValue(invoiceNode, "fhir:id/@value");
                data.Invoice.Status = GetAttributeValue(invoiceNode, "fhir:status/@value");

                // Extract total gross amount
                var totalGrossNode = invoiceNode.SelectSingleNode("fhir:totalGross", _namespaceManager);
                if (totalGrossNode != null)
                {
                    var totalValue = GetAttributeValue(totalGrossNode, "fhir:value/@value");
                    if (decimal.TryParse(totalValue, out var total))
                    {
                        data.Invoice.TotalGross = total;
                    }
                    data.Invoice.Currency = GetAttributeValue(totalGrossNode, "fhir:currency/@value");

                    // Extract copayment from extension
                    var copaymentExtension = totalGrossNode.SelectSingleNode("fhir:extension[@url='http://fhir.abda.de/eRezeptAbgabedaten/StructureDefinition/DAV-EX-ERP-Gesamtzuzahlung']", _namespaceManager);
                    if (copaymentExtension != null)
                    {
                        var copaymentValue = GetAttributeValue(copaymentExtension, "fhir:valueMoney/fhir:value/@value");
                        if (decimal.TryParse(copaymentValue, out var copayment))
                        {
                            data.Invoice.Copayment = copayment;
                        }
                    }
                }

                // Extract line items
                ExtractLineItems(invoiceNode, data.Invoice);
            }
        }

        private void ExtractLineItems(XmlNode invoiceNode, InvoiceInfo invoice)
        {
            var lineItemNodes = invoiceNode.SelectNodes("fhir:lineItem", _namespaceManager);
            if (lineItemNodes != null)
            {
                foreach (XmlNode lineItemNode in lineItemNodes)
                {
                    var lineItem = new LineItemInfo();

                    var sequenceValue = GetAttributeValue(lineItemNode, "fhir:sequence/@value");
                    if (int.TryParse(sequenceValue, out var sequence))
                    {
                        lineItem.Sequence = sequence;
                    }

                    // Extract PZN
                    var chargeItemNode = lineItemNode.SelectSingleNode("fhir:chargeItemCodeableConcept/fhir:coding", _namespaceManager);
                    if (chargeItemNode != null)
                    {
                        lineItem.PZN = GetAttributeValue(chargeItemNode, "fhir:code/@value");
                    }

                    // Extract price information
                    var priceComponentNode = lineItemNode.SelectSingleNode("fhir:priceComponent", _namespaceManager);
                    if (priceComponentNode != null)
                    {
                        var amountNode = priceComponentNode.SelectSingleNode("fhir:amount", _namespaceManager);
                        if (amountNode != null)
                        {
                            var amountValue = GetAttributeValue(amountNode, "fhir:value/@value");
                            if (decimal.TryParse(amountValue, out var amount))
                            {
                                lineItem.Amount = amount;
                            }
                            lineItem.Currency = GetAttributeValue(amountNode, "fhir:currency/@value");
                        }

                        // Extract VAT rate
                        var vatExtension = priceComponentNode.SelectSingleNode("fhir:extension[@url='http://fhir.abda.de/eRezeptAbgabedaten/StructureDefinition/DAV-EX-ERP-MwStSatz']", _namespaceManager);
                        if (vatExtension != null)
                        {
                            var vatValue = GetAttributeValue(vatExtension, "fhir:valueDecimal/@value");
                            if (decimal.TryParse(vatValue, out var vat))
                            {
                                lineItem.VatRate = vat;
                            }
                        }

                        // Extract copayment information
                        var copaymentExtension = priceComponentNode.SelectSingleNode("fhir:extension[@url='http://fhir.abda.de/eRezeptAbgabedaten/StructureDefinition/DAV-EX-ERP-KostenVersicherter']", _namespaceManager);
                        if (copaymentExtension != null)
                        {
                            var categoryNode = copaymentExtension.SelectSingleNode("fhir:extension[@url='Kategorie']/fhir:valueCodeableConcept/fhir:coding", _namespaceManager);
                            if (categoryNode != null)
                            {
                                lineItem.CopaymentCategory = GetAttributeValue(categoryNode, "fhir:code/@value");
                            }

                            var costNode = copaymentExtension.SelectSingleNode("fhir:extension[@url='Kostenbetrag']/fhir:valueMoney", _namespaceManager);
                            if (costNode != null)
                            {
                                var costValue = GetAttributeValue(costNode, "fhir:value/@value");
                                if (decimal.TryParse(costValue, out var cost))
                                {
                                    lineItem.CopaymentAmount = cost;
                                }
                            }
                        }
                    }

                    // Extract Zusatzattribute
                    ExtractZusatzattribute(lineItemNode, lineItem);

                    invoice.LineItems.Add(lineItem);
                }
            }
        }

        private void ExtractZusatzattribute(XmlNode lineItemNode, LineItemInfo lineItem)
        {
            var zusatzExtension = lineItemNode.SelectSingleNode("fhir:extension[@url='http://fhir.abda.de/eRezeptAbgabedaten/StructureDefinition/DAV-EX-ERP-Zusatzattribute']", _namespaceManager);
            if (zusatzExtension != null)
            {
                var famExtension = zusatzExtension.SelectSingleNode("fhir:extension[@url='ZusatzattributFAM']", _namespaceManager);
                if (famExtension != null)
                {
                    // Extract Markt
                    var marktExtension = famExtension.SelectSingleNode("fhir:extension[@url='Markt']", _namespaceManager);
                    if (marktExtension != null)
                    {
                        lineItem.Zusatzattribute.Markt.Schluessel = GetCodeFromExtension(marktExtension, "Schluessel");
                        lineItem.Zusatzattribute.Markt.Gruppe = GetCodeFromExtension(marktExtension, "Gruppe");
                    }

                    // Extract Rabattvertragserfuellung
                    var rabattExtension = famExtension.SelectSingleNode("fhir:extension[@url='Rabattvertragserfuellung']", _namespaceManager);
                    if (rabattExtension != null)
                    {
                        lineItem.Zusatzattribute.Rabattvertragserfuellung.Gruppe = GetCodeFromExtension(rabattExtension, "Gruppe");
                        lineItem.Zusatzattribute.Rabattvertragserfuellung.Schluessel = GetCodeFromExtension(rabattExtension, "Schluessel");
                    }

                    // Extract PreisguenstigesFAM
                    var preisExtension = famExtension.SelectSingleNode("fhir:extension[@url='PreisguenstigesFAM']", _namespaceManager);
                    if (preisExtension != null)
                    {
                        lineItem.Zusatzattribute.PreisguenstigesFAM.Gruppe = GetCodeFromExtension(preisExtension, "Gruppe");
                        lineItem.Zusatzattribute.PreisguenstigesFAM.Schluessel = GetCodeFromExtension(preisExtension, "Schluessel");
                    }

                    // Extract ImportFAM
                    var importExtension = famExtension.SelectSingleNode("fhir:extension[@url='ImportFAM']", _namespaceManager);
                    if (importExtension != null)
                    {
                        lineItem.Zusatzattribute.ImportFAM.Gruppe = GetCodeFromExtension(importExtension, "Gruppe");
                        lineItem.Zusatzattribute.ImportFAM.Schluessel = GetCodeFromExtension(importExtension, "Schluessel");
                    }
                }
            }
        }

        private string GetCodeFromExtension(XmlNode extensionNode, string childExtensionUrl)
        {
            var childExtension = extensionNode.SelectSingleNode($"fhir:extension[@url='{childExtensionUrl}']/fhir:valueCodeableConcept/fhir:coding", _namespaceManager);
            return childExtension != null ? GetAttributeValue(childExtension, "fhir:code/@value") : string.Empty;
        }

        private string GetAttributeValue(XmlNode node, string xpath)
        {
            var attributeNode = node?.SelectSingleNode(xpath, _namespaceManager);
            return attributeNode?.Value ?? string.Empty;
        }
    }
}
