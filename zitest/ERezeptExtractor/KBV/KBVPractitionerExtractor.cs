using System.Xml;
using System.Xml.XPath;
using ERezeptAbgabeExtractor.Models;

namespace ERezeptAbgabeExtractor.KBV
{
    /// <summary>
    /// Specialized extractor for KBV (Kassenärztliche Bundesvereinigung) practitioner data
    /// Based on requirements from "Rezepte - Tabellenblatt10.csv"
    /// 
    /// Implements extraction logic for:
    /// - la_nr (ID 42): Doctor number of prescribing person
    /// - la_nr_v (ID 52): Doctor number of responsible person
    /// 
    /// la_nr_v Logic:
    /// - Attribut: la_nr_v, ID: 52
    /// - Beschreibung: Arztnummer der verantwortlichen Person
    /// - Länge: leer oder 9, Typ: alphanumerisch
    /// - XPATH: fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value
    /// - Logic: LANR des Verantwortlichen (Typ 00 oder 04) - nutzen, falls in Attribut 'lanr' Assistenz (Typ 03) 
    ///   gefunden wird, ansonsten bleibt Attribut leer!
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
            
            // Extract coverage payer information for kostentr_bg
            ExtractCoveragePayerData(xmlDoc, data);
            
            return data;
        }

        /// <summary>
        /// Extracts practitioner data according to KBV requirements from XML string
        /// </summary>
        /// <param name="xmlContent">The FHIR XML content as string</param>
        /// <returns>Extended eRezept data with practitioner information</returns>
        public ExtendedERezeptData ExtractKBVData(string xmlContent)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            return ExtractKBVData(xmlDoc);
        }

        private void CopyStandardData(ERezeptAbgabeData source, ExtendedERezeptData target)
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
                
                // Alternative: Use direct XPath extraction for validation
                ValidateWithDirectXPath(xmlDoc, data);
            }
        }

        /// <summary>
        /// Validate LANR extraction using direct XPath as specified in the requirements
        /// This implements the exact XPath from la_nr_v (ID 52) specification
        /// </summary>
        private void ValidateWithDirectXPath(XmlDocument xmlDoc, ExtendedERezeptData data)
        {
            // Extract assistant LANR using direct XPath
            var assistantLANR = ExtractAssistantPersonLANR(xmlDoc);
            
            // Extract responsible person LANR using direct XPath  
            var responsibleLANR = ExtractResponsiblePersonLANR(xmlDoc);
            
            // Apply la_nr_v logic: Use responsible LANR only if assistant LANR is used for la_nr
            if (!string.IsNullOrEmpty(assistantLANR))
            {
                // Assistant found - ensure la_nr uses assistant LANR and la_nr_v uses responsible LANR
                data.Practitioner.LANR = assistantLANR;
                if (!string.IsNullOrEmpty(responsibleLANR))
                {
                    data.Practitioner.LANR_Responsible = responsibleLANR;
                }
            }
            else if (!string.IsNullOrEmpty(responsibleLANR))
            {
                // Only responsible found - use for la_nr, leave la_nr_v empty
                data.Practitioner.LANR = responsibleLANR;
                data.Practitioner.LANR_Responsible = string.Empty;
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
                        
                        // Extract associated LANR - use more specific XPath for each qualification type
                        // This implements the logic for la_nr_v (ID 52) according to KBV requirements
                        if (qualification.TypeCode == "00" || qualification.TypeCode == "03" || qualification.TypeCode == "04")
                        {
                            var lanrNode = practitionerNode.SelectSingleNode("fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']", _namespaceManager);
                            if (lanrNode != null)
                            {
                                qualification.AssociatedLANR = GetAttributeValue(lanrNode, "fhir:value/@value");
                            }
                        }
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
        /// Implements la_nr (ID 42) and la_nr_v (ID 52) extraction logic
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
            // la_nr_v (ID 52): LANR of responsible person - only filled if assistant is found in la_nr
            if (assistantQual != null && !string.IsNullOrEmpty(assistantQual.AssociatedLANR))
            {
                // Assistant found - use assistant LANR for la_nr (ID 42)
                practitioner.LANR = assistantQual.AssociatedLANR;
                
                // la_nr_v (ID 52): Use responsible LANR for la_nr_v when assistant is in la_nr
                // XPath: fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' 
                // and (fhir:code/@value='00' or fhir:code/@value='04')]/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value
                if (responsibleQual != null && !string.IsNullOrEmpty(responsibleQual.AssociatedLANR))
                {
                    practitioner.LANR_Responsible = responsibleQual.AssociatedLANR;
                }
            }
            else if (responsibleQual != null && !string.IsNullOrEmpty(responsibleQual.AssociatedLANR))
            {
                // No assistant found - use responsible LANR for la_nr (ID 42)
                practitioner.LANR = responsibleQual.AssociatedLANR;
                // la_nr_v (ID 52) remains empty when no assistant is found
                practitioner.LANR_Responsible = string.Empty;
            }
            else
            {
                // If no LANR found, set default value as specified in CSV
                practitioner.LANR = "000000000";
                practitioner.LANR_Responsible = string.Empty;
            }
        }

        /// <summary>
        /// Extract coverage payer data for kostentr_bg field
        /// Looks for Coverage resources with payer type UK or BG and extracts alternative IK number
        /// </summary>
        private void ExtractCoveragePayerData(XmlDocument xmlDoc, ExtendedERezeptData data)
        {
            // XPath to find Coverage resources with UK or BG payer types
            var xpath = "//fhir:entry/fhir:resource/fhir:Coverage[" +
                       "fhir:type/fhir:coding/fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Payor_Type_KBV' and " +
                       "(fhir:type/fhir:coding/fhir:code/@value='UK' or fhir:type/fhir:coding/fhir:code/@value='BG')]";
            
            var coverageNodes = xmlDoc.SelectNodes(xpath, _namespaceManager);
            
            if (coverageNodes != null && coverageNodes.Count > 0)
            {
                foreach (XmlNode coverageNode in coverageNodes)
                {
                    // Extract alternative IK from the payer identifier extension
                    var alternativeIK = ExtractAlternativeIK(coverageNode);
                    if (!string.IsNullOrEmpty(alternativeIK))
                    {
                        data.KostentraegerBG = alternativeIK;
                        break; // Take the first valid alternative IK found
                    }
                }
            }
        }

        /// <summary>
        /// Extract alternative IK number from Coverage payer identifier extension
        /// </summary>
        private string ExtractAlternativeIK(XmlNode coverageNode)
        {
            // XPath to find the alternative IK extension
            var xpath = "fhir:payor/fhir:identifier/fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_FOR_Alternative_IK']/" +
                       "fhir:valueIdentifier[fhir:system/@value='http://fhir.de/sid/arge-ik/iknr']/fhir:value/@value";
            
            var ikNode = coverageNode.SelectSingleNode(xpath, _namespaceManager);
            return ikNode?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extract LANR of responsible person (Type 00 or 04) according to la_nr_v (ID 52) specification
        /// XPath: fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' 
        /// and (fhir:code/@value='00' or fhir:code/@value='04')]/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value
        /// </summary>
        private string ExtractResponsiblePersonLANR(XmlDocument xmlDoc)
        {
            var xpath = "//fhir:entry/fhir:resource/fhir:Practitioner[fhir:qualification/fhir:code/fhir:coding[" +
                       "fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and " +
                       "(fhir:code/@value='00' or fhir:code/@value='04')]]/fhir:identifier[" +
                       "fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value/@value";
            
            var lanrNode = xmlDoc.SelectSingleNode(xpath, _namespaceManager);
            return lanrNode?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extract LANR of assistant person (Type 03) for la_nr (ID 42)
        /// </summary>
        private string ExtractAssistantPersonLANR(XmlDocument xmlDoc)
        {
            var xpath = "//fhir:entry/fhir:resource/fhir:Practitioner[fhir:qualification/fhir:code/fhir:coding[" +
                       "fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and " +
                       "fhir:code/@value='03']]/fhir:identifier[" +
                       "fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value/@value";
            
            var lanrNode = xmlDoc.SelectSingleNode(xpath, _namespaceManager);
            return lanrNode?.Value ?? string.Empty;
        }

        private string GetAttributeValue(XmlNode node, string xpath)
        {
            var attributeNode = node?.SelectSingleNode(xpath, _namespaceManager);
            return attributeNode?.Value ?? string.Empty;
        }
    }
}
