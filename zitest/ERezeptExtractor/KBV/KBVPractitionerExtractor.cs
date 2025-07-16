using System.Xml;
using System.Xml.XPath;
using ERezeptExtractor.Models;

namespace ERezeptExtractor.KBV
{
    /// <summary>
    /// Specialized extractor for KBV (Kassenärztliche Bundesvereinigung) practitioner data
    /// Based on requirements from "Rezepte - Tabellenblatt10.csv"
    /// </summary>
    public class KBVPractitionerExtractor
    {
        private readonly XmlNamespaceManager _namespaceManager;

        public KBVPractitionerExtractor()
        {
            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("fhir", "http://hl7.org/fhir");
        }

        /// <summary>
        /// Extracts practitioner data according to KBV requirements
        /// </summary>
        /// <param name="xmlDoc">The FHIR XML document</param>
        /// <returns>Extended eRezept data with practitioner information</returns>
        public ExtendedERezeptData ExtractKBVData(XmlDocument xmlDoc)
        {
            var data = new ExtendedERezeptData();
            
            // First extract standard eRezept data
            var standardExtractor = new ERezeptExtractor();
            var standardData = standardExtractor.ExtractFromXmlDocument(xmlDoc);
            
            // Copy standard data
            CopyStandardData(standardData, data);
            
            // Extract KBV-specific practitioner data
            ExtractPractitionerData(xmlDoc, data);
            
            return data;
        }

        private void CopyStandardData(ERezeptData source, ExtendedERezeptData target)
        {
            target.BundleId = source.BundleId;
            target.PrescriptionId = source.PrescriptionId;
            target.Timestamp = source.Timestamp;
            target.Pharmacy = source.Pharmacy;
            target.MedicationDispense = source.MedicationDispense;
            target.Invoice = source.Invoice;
        }

        /// <summary>
        /// Extract practitioner data according to CSV requirements
        /// Implements logic for la_nr (ID 42) and la_nr_v (ID 52)
        /// </summary>
        private void ExtractPractitionerData(XmlDocument xmlDoc, ExtendedERezeptData data)
        {
            // Find all Practitioner resources
            var practitionerNodes = xmlDoc.SelectNodes("//fhir:entry/fhir:resource/fhir:Practitioner", _namespaceManager);
            
            if (practitionerNodes != null && practitionerNodes.Count > 0)
            {
                // Process each practitioner to find the appropriate LANR
                foreach (XmlNode practitionerNode in practitionerNodes)
                {
                    ProcessPractitioner(practitionerNode, data);
                }
                
                // Apply KBV-specific logic for LANR assignment
                ApplyKBVLANRLogic(data);
            }
        }

        private void ProcessPractitioner(XmlNode practitionerNode, ExtendedERezeptData data)
        {
            var practitioner = data.Practitioner;
            
            // Extract basic practitioner info
            practitioner.Id = GetAttributeValue(practitionerNode, "fhir:id/@value");
            
            // Extract name information
            ExtractNameInfo(practitionerNode, practitioner);
            
            // Extract qualifications and associated LANRs
            ExtractQualifications(practitionerNode, practitioner);
            
            // Extract address if available
            ExtractPractitionerAddress(practitionerNode, practitioner);
        }

        private void ExtractNameInfo(XmlNode practitionerNode, PractitionerInfo practitioner)
        {
            var nameNode = practitionerNode.SelectSingleNode("fhir:name", _namespaceManager);
            if (nameNode != null)
            {
                practitioner.Name.Family = GetAttributeValue(nameNode, "fhir:family/@value");
                practitioner.Name.Given = GetAttributeValue(nameNode, "fhir:given/@value");
                practitioner.Name.Prefix = GetAttributeValue(nameNode, "fhir:prefix/@value");
                practitioner.Name.Suffix = GetAttributeValue(nameNode, "fhir:suffix/@value");
            }
        }

        private void ExtractQualifications(XmlNode practitionerNode, PractitionerInfo practitioner)
        {
            var qualificationNodes = practitionerNode.SelectNodes("fhir:qualification", _namespaceManager);
            if (qualificationNodes != null)
            {
                foreach (XmlNode qualNode in qualificationNodes)
                {
                    var qualification = new QualificationInfo();
                    
                    // Extract qualification type
                    var codeNode = qualNode.SelectSingleNode("fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type']", _namespaceManager);
                    if (codeNode != null)
                    {
                        qualification.TypeCode = GetAttributeValue(codeNode, "fhir:code/@value");
                        qualification.TypeDisplay = GetAttributeValue(codeNode, "fhir:display/@value");
                    }
                    
                    // Extract associated LANR
                    var lanrNode = practitionerNode.SelectSingleNode("fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']", _namespaceManager);
                    if (lanrNode != null)
                    {
                        qualification.AssociatedLANR = GetAttributeValue(lanrNode, "fhir:value/@value");
                    }
                    
                    practitioner.Qualifications.Add(qualification);
                }
            }
        }

        private void ExtractPractitionerAddress(XmlNode practitionerNode, PractitionerInfo practitioner)
        {
            var addressNode = practitionerNode.SelectSingleNode("fhir:address", _namespaceManager);
            if (addressNode != null)
            {
                practitioner.Address.City = GetAttributeValue(addressNode, "fhir:city/@value");
                practitioner.Address.PostalCode = GetAttributeValue(addressNode, "fhir:postalCode/@value");
                practitioner.Address.Country = GetAttributeValue(addressNode, "fhir:country/@value");
                
                var lineNode = addressNode.SelectSingleNode("fhir:line", _namespaceManager);
                if (lineNode != null)
                {
                    var streetExtension = lineNode.SelectSingleNode("fhir:extension[@url='http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-streetName']", _namespaceManager);
                    if (streetExtension != null)
                    {
                        practitioner.Address.Street = GetAttributeValue(streetExtension, "fhir:valueString/@value");
                    }
                    
                    var houseNumberExtension = lineNode.SelectSingleNode("fhir:extension[@url='http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-houseNumber']", _namespaceManager);
                    if (houseNumberExtension != null)
                    {
                        practitioner.Address.HouseNumber = GetAttributeValue(houseNumberExtension, "fhir:valueString/@value");
                    }
                }
            }
        }

        /// <summary>
        /// Apply KBV-specific logic for LANR assignment according to CSV requirements
        /// </summary>
        private void ApplyKBVLANRLogic(ExtendedERezeptData data)
        {
            var practitioner = data.Practitioner;
            
            // Find assistant qualification (Type 03)
            var assistantQual = practitioner.Qualifications.FirstOrDefault(q => q.TypeCode == "03");
            
            // Find responsible doctor qualification (Type 00 or 04)
            var responsibleQual = practitioner.Qualifications.FirstOrDefault(q => q.TypeCode == "00" || q.TypeCode == "04");
            
            // Apply logic according to CSV requirements:
            // la_nr (ID 42): Use assistant LANR if available, otherwise use responsible LANR
            if (assistantQual != null && !string.IsNullOrEmpty(assistantQual.AssociatedLANR))
            {
                practitioner.LANR = assistantQual.AssociatedLANR;
                
                // la_nr_v (ID 52): If assistant found, use responsible LANR for la_nr_v
                if (responsibleQual != null && !string.IsNullOrEmpty(responsibleQual.AssociatedLANR))
                {
                    practitioner.LANR_Responsible = responsibleQual.AssociatedLANR;
                }
            }
            else if (responsibleQual != null && !string.IsNullOrEmpty(responsibleQual.AssociatedLANR))
            {
                practitioner.LANR = responsibleQual.AssociatedLANR;
                // la_nr_v remains empty if no assistant was found
            }
            else
            {
                // If no LANR found, set default value as specified in CSV
                practitioner.LANR = "000000000";
            }
        }

        private string GetAttributeValue(XmlNode node, string xpath)
        {
            var attributeNode = node?.SelectSingleNode(xpath, _namespaceManager);
            return attributeNode?.Value ?? string.Empty;
        }
    }
}
