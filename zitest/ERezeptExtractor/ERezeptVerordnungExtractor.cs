using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using ERezeptVerordnungExtractor.Models;

namespace ERezeptVerordnungExtractor
{
    /// <summary>
    /// Extractor for eRezept Verordnung (prescription) FHIR XML files
    /// </summary>
    public class ERezeptVerordnungExtractor
    {
        private readonly XmlNamespaceManager _namespaceManager;
        
        public ERezeptVerordnungExtractor()
        {
            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("fhir", "http://hl7.org/fhir");
        }

        /// <summary>
        /// Extracts data from an eRezept Verordnung FHIR XML string
        /// </summary>
        /// <param name="xmlContent">The XML content as string</param>
        /// <returns>Extracted eRezept Verordnung data</returns>
        public ERezeptVerordnungData ExtractFromXml(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("XML content cannot be null or empty", nameof(xmlContent));

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            
            return ExtractFromXmlDocument(xmlDoc);
        }

        /// <summary>
        /// Extracts data from an eRezept Verordnung FHIR XML file
        /// </summary>
        /// <param name="filePath">Path to the XML file</param>
        /// <returns>Extracted eRezept Verordnung data</returns>
        public ERezeptVerordnungData ExtractFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var xmlContent = File.ReadAllText(filePath);
            return ExtractFromXml(xmlContent);
        }

        /// <summary>
        /// Extracts data from an eRezept Verordnung FHIR XML document
        /// </summary>
        /// <param name="xmlDoc">The XML document</param>
        /// <returns>Extracted eRezept Verordnung data</returns>
        public ERezeptVerordnungData ExtractFromXmlDocument(XmlDocument xmlDoc)
        {
            var data = new ERezeptVerordnungData();
            
            ExtractBundleInfo(xmlDoc, data);
            ExtractPractitionerInfo(xmlDoc, data);
            ExtractOrganizationInfo(xmlDoc, data);
            ExtractPatientInfo(xmlDoc, data);
            ExtractCoverageInfo(xmlDoc, data);
            ExtractMedicationInfo(xmlDoc, data);
            ExtractMedicationRequestInfo(xmlDoc, data);
            
            return data;
        }

        /// <summary>
        /// Extracts bundle information (ID, prescription ID, timestamp)
        /// </summary>
        private void ExtractBundleInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            // Bundle ID
            var bundleIdNode = xmlDoc.SelectSingleNode("//fhir:Bundle/fhir:id/@value", _namespaceManager);
            data.BundleId = bundleIdNode?.Value ?? string.Empty;

            // Prescription ID
            var prescriptionIdNode = xmlDoc.SelectSingleNode("//fhir:Bundle/fhir:identifier[fhir:system/@value='https://gematik.de/fhir/erp/NamingSystem/GEM_ERP_NS_PrescriptionId']/fhir:value/@value", _namespaceManager);
            data.PrescriptionId = prescriptionIdNode?.Value ?? string.Empty;

            // Timestamp
            var timestampNode = xmlDoc.SelectSingleNode("//fhir:Bundle/fhir:timestamp/@value", _namespaceManager);
            if (timestampNode != null && DateTime.TryParse(timestampNode.Value, out var timestamp))
            {
                data.Timestamp = timestamp;
            }
        }

        /// <summary>
        /// Extracts practitioner (doctor) information
        /// </summary>
        private void ExtractPractitionerInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            var practitionerNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Practitioner", _namespaceManager);
            if (practitionerNode == null) return;

            var practitioner = data.Practitioner;

            // ID
            var idNode = practitionerNode.SelectSingleNode("fhir:id/@value", _namespaceManager);
            practitioner.Id = idNode?.Value ?? string.Empty;

            // LANR
            var lanrNode = practitionerNode.SelectSingleNode("fhir:identifier[fhir:type/fhir:coding/fhir:code/@value='LANR']/fhir:value/@value", _namespaceManager);
            practitioner.LANR = lanrNode?.Value ?? string.Empty;

            // Name
            var nameNode = practitionerNode.SelectSingleNode("fhir:name[@use='official']", _namespaceManager);
            if (nameNode != null)
            {
                var familyNode = nameNode.SelectSingleNode("fhir:family/@value", _namespaceManager);
                practitioner.Name.Family = familyNode?.Value ?? string.Empty;

                var givenNode = nameNode.SelectSingleNode("fhir:given/@value", _namespaceManager);
                practitioner.Name.Given = givenNode?.Value ?? string.Empty;

                var prefixNode = nameNode.SelectSingleNode("fhir:prefix/@value", _namespaceManager);
                practitioner.Name.Prefix = prefixNode?.Value ?? string.Empty;
            }

            // Qualifications
            var qualificationNodes = practitionerNode.SelectNodes("fhir:qualification", _namespaceManager);
            if (qualificationNodes != null)
            {
                foreach (XmlNode qualNode in qualificationNodes)
                {
                    var qualification = new QualificationInfo();
                    
                    var codeNode = qualNode.SelectSingleNode("fhir:code/fhir:coding/fhir:code/@value", _namespaceManager);
                    qualification.TypeCode = codeNode?.Value ?? string.Empty;

                    var displayNode = qualNode.SelectSingleNode("fhir:code/fhir:coding/fhir:display/@value", _namespaceManager);
                    qualification.TypeDisplay = displayNode?.Value ?? string.Empty;

                    var textNode = qualNode.SelectSingleNode("fhir:code/fhir:text/@value", _namespaceManager);
                    qualification.Text = textNode?.Value ?? string.Empty;

                    practitioner.Qualifications.Add(qualification);
                }
            }
        }

        /// <summary>
        /// Extracts organization (practice/clinic) information
        /// </summary>
        private void ExtractOrganizationInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            var organizationNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Organization", _namespaceManager);
            if (organizationNode == null) return;

            var organization = data.Organization;

            // ID
            var idNode = organizationNode.SelectSingleNode("fhir:id/@value", _namespaceManager);
            organization.Id = idNode?.Value ?? string.Empty;

            // BSNR
            var bsnrNode = organizationNode.SelectSingleNode("fhir:identifier[fhir:type/fhir:coding/fhir:code/@value='BSNR']/fhir:value/@value", _namespaceManager);
            organization.BSNR = bsnrNode?.Value ?? string.Empty;

            // Name
            var nameNode = organizationNode.SelectSingleNode("fhir:name/@value", _namespaceManager);
            organization.Name = nameNode?.Value ?? string.Empty;

            // Telecoms
            var telecomNodes = organizationNode.SelectNodes("fhir:telecom", _namespaceManager);
            if (telecomNodes != null)
            {
                foreach (XmlNode telecomNode in telecomNodes)
                {
                    var telecom = new TelecomInfo();
                    
                    var systemNode = telecomNode.SelectSingleNode("fhir:system/@value", _namespaceManager);
                    telecom.System = systemNode?.Value ?? string.Empty;

                    var valueNode = telecomNode.SelectSingleNode("fhir:value/@value", _namespaceManager);
                    telecom.Value = valueNode?.Value ?? string.Empty;

                    organization.Telecoms.Add(telecom);
                }
            }

            // Address
            var addressNode = organizationNode.SelectSingleNode("fhir:address", _namespaceManager);
            if (addressNode != null)
            {
                ExtractAddress(addressNode, organization.Address);
            }
        }

        /// <summary>
        /// Extracts patient information
        /// </summary>
        private void ExtractPatientInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            var patientNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Patient", _namespaceManager);
            if (patientNode == null) return;

            var patient = data.Patient;

            // ID
            var idNode = patientNode.SelectSingleNode("fhir:id/@value", _namespaceManager);
            patient.Id = idNode?.Value ?? string.Empty;

            // Insurance number
            var insuranceNode = patientNode.SelectSingleNode("fhir:identifier[fhir:type/fhir:coding/fhir:code/@value='GKV']/fhir:value/@value", _namespaceManager);
            patient.InsuranceNumber = insuranceNode?.Value ?? string.Empty;

            // Name
            var nameNode = patientNode.SelectSingleNode("fhir:name[@use='official']", _namespaceManager);
            if (nameNode != null)
            {
                var familyNode = nameNode.SelectSingleNode("fhir:family/@value", _namespaceManager);
                patient.Name.Family = familyNode?.Value ?? string.Empty;

                var givenNode = nameNode.SelectSingleNode("fhir:given/@value", _namespaceManager);
                patient.Name.Given = givenNode?.Value ?? string.Empty;
            }

            // Birth date
            var birthDateNode = patientNode.SelectSingleNode("fhir:birthDate/@value", _namespaceManager);
            if (birthDateNode != null && DateTime.TryParse(birthDateNode.Value, out var birthDate))
            {
                patient.BirthDate = birthDate;
            }

            // Address
            var addressNode = patientNode.SelectSingleNode("fhir:address", _namespaceManager);
            if (addressNode != null)
            {
                ExtractAddress(addressNode, patient.Address);
            }
        }

        /// <summary>
        /// Extracts coverage (insurance) information
        /// </summary>
        private void ExtractCoverageInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            var coverageNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Coverage", _namespaceManager);
            if (coverageNode == null) return;

            var coverage = data.Coverage;

            // ID
            var idNode = coverageNode.SelectSingleNode("fhir:id/@value", _namespaceManager);
            coverage.Id = idNode?.Value ?? string.Empty;

            // Status
            var statusNode = coverageNode.SelectSingleNode("fhir:status/@value", _namespaceManager);
            coverage.Status = statusNode?.Value ?? string.Empty;

            // Type
            var typeNode = coverageNode.SelectSingleNode("fhir:type/fhir:coding/fhir:code/@value", _namespaceManager);
            coverage.Type = typeNode?.Value ?? string.Empty;

            // Payor IK
            var payorIKNode = coverageNode.SelectSingleNode("fhir:payor/fhir:identifier/fhir:value/@value", _namespaceManager);
            coverage.PayorIK = payorIKNode?.Value ?? string.Empty;

            // Alternative IK
            var altIKNode = coverageNode.SelectSingleNode("fhir:payor/fhir:identifier/fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_FOR_Alternative_IK']/fhir:valueIdentifier/fhir:value/@value", _namespaceManager);
            coverage.AlternativeIK = altIKNode?.Value ?? string.Empty;

            // Payor name
            var payorNameNode = coverageNode.SelectSingleNode("fhir:payor/fhir:display/@value", _namespaceManager);
            coverage.PayorName = payorNameNode?.Value ?? string.Empty;

            // Extensions
            var personenGruppeNode = coverageNode.SelectSingleNode("fhir:extension[@url='http://fhir.de/StructureDefinition/gkv/besondere-personengruppe']/fhir:valueCoding/fhir:code/@value", _namespaceManager);
            coverage.PersonenGruppe = personenGruppeNode?.Value ?? string.Empty;

            var dmpNode = coverageNode.SelectSingleNode("fhir:extension[@url='http://fhir.de/StructureDefinition/gkv/dmp-kennzeichen']/fhir:valueCoding/fhir:code/@value", _namespaceManager);
            coverage.DmpKennzeichen = dmpNode?.Value ?? string.Empty;

            var versichertenArtNode = coverageNode.SelectSingleNode("fhir:extension[@url='http://fhir.de/StructureDefinition/gkv/versichertenart']/fhir:valueCoding/fhir:code/@value", _namespaceManager);
            coverage.VersichertenArt = versichertenArtNode?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extracts medication information
        /// </summary>
        private void ExtractMedicationInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            var medicationNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:Medication", _namespaceManager);
            if (medicationNode == null) return;

            var medication = data.Medication;

            // ID
            var idNode = medicationNode.SelectSingleNode("fhir:id/@value", _namespaceManager);
            medication.Id = idNode?.Value ?? string.Empty;

            // PZN
            var pznNode = medicationNode.SelectSingleNode("fhir:code/fhir:coding[fhir:system/@value='http://fhir.de/CodeSystem/ifa/pzn']/fhir:code/@value", _namespaceManager);
            medication.PZN = pznNode?.Value ?? string.Empty;

            // Name
            var nameNode = medicationNode.SelectSingleNode("fhir:code/fhir:text/@value", _namespaceManager);
            medication.Name = nameNode?.Value ?? string.Empty;

            // Form
            var formNode = medicationNode.SelectSingleNode("fhir:form/fhir:coding/fhir:code/@value", _namespaceManager);
            medication.Form = formNode?.Value ?? string.Empty;

            // Category
            var categoryNode = medicationNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_Medication_Category']/fhir:valueCoding/fhir:code/@value", _namespaceManager);
            medication.Category = categoryNode?.Value ?? string.Empty;

            // Norm size
            var normSizeNode = medicationNode.SelectSingleNode("fhir:extension[@url='http://fhir.de/StructureDefinition/normgroesse']/fhir:valueCode/@value", _namespaceManager);
            medication.NormSize = normSizeNode?.Value ?? string.Empty;

            // Is vaccine
            var vaccineNode = medicationNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_Medication_Vaccine']/fhir:valueBoolean/@value", _namespaceManager);
            if (vaccineNode != null && bool.TryParse(vaccineNode.Value, out var isVaccine))
            {
                medication.IsVaccine = isVaccine;
            }

            // Medication type
            var typeNode = medicationNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_Base_Medication_Type']/fhir:valueCodeableConcept/fhir:coding/fhir:display/@value", _namespaceManager);
            medication.MedicationType = typeNode?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extracts medication request (prescription) information
        /// </summary>
        private void ExtractMedicationRequestInfo(XmlDocument xmlDoc, ERezeptVerordnungData data)
        {
            var requestNode = xmlDoc.SelectSingleNode("//fhir:entry/fhir:resource/fhir:MedicationRequest", _namespaceManager);
            if (requestNode == null) return;

            var request = data.MedicationRequest;

            // ID
            var idNode = requestNode.SelectSingleNode("fhir:id/@value", _namespaceManager);
            request.Id = idNode?.Value ?? string.Empty;

            // Status
            var statusNode = requestNode.SelectSingleNode("fhir:status/@value", _namespaceManager);
            request.Status = statusNode?.Value ?? string.Empty;

            // Intent
            var intentNode = requestNode.SelectSingleNode("fhir:intent/@value", _namespaceManager);
            request.Intent = intentNode?.Value ?? string.Empty;

            // Authored on
            var authoredOnNode = requestNode.SelectSingleNode("fhir:authoredOn/@value", _namespaceManager);
            if (authoredOnNode != null && DateTime.TryParse(authoredOnNode.Value, out var authoredOn))
            {
                request.AuthoredOn = authoredOn;
            }

            // Status co-payment
            var statusCoPaymentNode = requestNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_FOR_StatusCoPayment']/fhir:valueCoding/fhir:code/@value", _namespaceManager);
            request.StatusCoPayment = statusCoPaymentNode?.Value ?? string.Empty;

            // Emergency services fee
            var emergencyFeeNode = requestNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_EmergencyServicesFee']/fhir:valueBoolean/@value", _namespaceManager);
            if (emergencyFeeNode != null && bool.TryParse(emergencyFeeNode.Value, out var emergencyFee))
            {
                request.EmergencyServicesFee = emergencyFee;
            }

            // BVG
            var bvgNode = requestNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_BVG']/fhir:valueBoolean/@value", _namespaceManager);
            if (bvgNode != null && bool.TryParse(bvgNode.Value, out var bvg))
            {
                request.BVG = bvg;
            }

            // Accident info
            ExtractAccidentInfo(requestNode, request.Accident);

            // Multiple prescription info
            ExtractMultipleInfo(requestNode, request.Multiple);

            // Dosage info
            ExtractDosageInfo(requestNode, request.Dosage);

            // Dispense request info
            ExtractDispenseRequestInfo(requestNode, request.DispenseRequest);

            // Substitution info
            ExtractSubstitutionInfo(requestNode, request.Substitution);
        }

        /// <summary>
        /// Extracts accident information from medication request
        /// </summary>
        private void ExtractAccidentInfo(XmlNode requestNode, AccidentInfo accident)
        {
            var accidentExtension = requestNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_FOR_Accident']", _namespaceManager);
            if (accidentExtension == null) return;

            var kennzeichenNode = accidentExtension.SelectSingleNode("fhir:extension[@url='Unfallkennzeichen']/fhir:valueCoding/fhir:code/@value", _namespaceManager);
            accident.Kennzeichen = kennzeichenNode?.Value ?? string.Empty;

            var betriebNode = accidentExtension.SelectSingleNode("fhir:extension[@url='Unfallbetrieb']/fhir:valueString/@value", _namespaceManager);
            accident.Betrieb = betriebNode?.Value ?? string.Empty;

            var unfalltagNode = accidentExtension.SelectSingleNode("fhir:extension[@url='Unfalltag']/fhir:valueDate/@value", _namespaceManager);
            if (unfalltagNode != null && DateTime.TryParse(unfalltagNode.Value, out var unfalltag))
            {
                accident.Unfalltag = unfalltag;
            }
        }

        /// <summary>
        /// Extracts multiple prescription information from medication request
        /// </summary>
        private void ExtractMultipleInfo(XmlNode requestNode, MultipleInfo multiple)
        {
            var multipleExtension = requestNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_Multiple_Prescription']", _namespaceManager);
            if (multipleExtension == null) return;

            var kennzeichenNode = multipleExtension.SelectSingleNode("fhir:extension[@url='Kennzeichen']/fhir:valueBoolean/@value", _namespaceManager);
            if (kennzeichenNode != null && bool.TryParse(kennzeichenNode.Value, out var kennzeichen))
            {
                multiple.Kennzeichen = kennzeichen;
            }

            var numeratorNode = multipleExtension.SelectSingleNode("fhir:extension[@url='Nummerierung']/fhir:valueRatio/fhir:numerator/fhir:value/@value", _namespaceManager);
            if (numeratorNode != null && int.TryParse(numeratorNode.Value, out var numerator))
            {
                multiple.Numerator = numerator;
            }

            var denominatorNode = multipleExtension.SelectSingleNode("fhir:extension[@url='Nummerierung']/fhir:valueRatio/fhir:denominator/fhir:value/@value", _namespaceManager);
            if (denominatorNode != null && int.TryParse(denominatorNode.Value, out var denominator))
            {
                multiple.Denominator = denominator;
            }

            var validityPeriodNode = multipleExtension.SelectSingleNode("fhir:extension[@url='Zeitraum']", _namespaceManager);
            if (validityPeriodNode != null)
            {
                var startNode = validityPeriodNode.SelectSingleNode("fhir:valuePeriod/fhir:start/@value", _namespaceManager);
                if (startNode != null && DateTime.TryParse(startNode.Value, out var start))
                {
                    multiple.ValidityPeriodStart = start;
                }

                var endNode = validityPeriodNode.SelectSingleNode("fhir:valuePeriod/fhir:end/@value", _namespaceManager);
                if (endNode != null && DateTime.TryParse(endNode.Value, out var end))
                {
                    multiple.ValidityPeriodEnd = end;
                }
            }
        }

        /// <summary>
        /// Extracts dosage information from medication request
        /// </summary>
        private void ExtractDosageInfo(XmlNode requestNode, DosageInfo dosage)
        {
            var dosageNode = requestNode.SelectSingleNode("fhir:dosageInstruction", _namespaceManager);
            if (dosageNode == null) return;

            var dosageFlagNode = dosageNode.SelectSingleNode("fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_DosageFlag']/fhir:valueBoolean/@value", _namespaceManager);
            if (dosageFlagNode != null && bool.TryParse(dosageFlagNode.Value, out var dosageFlag))
            {
                dosage.DosageFlag = dosageFlag;
            }

            var textNode = dosageNode.SelectSingleNode("fhir:text/@value", _namespaceManager);
            dosage.Text = textNode?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extracts dispense request information from medication request
        /// </summary>
        private void ExtractDispenseRequestInfo(XmlNode requestNode, DispenseRequestInfo dispenseRequest)
        {
            var dispenseNode = requestNode.SelectSingleNode("fhir:dispenseRequest", _namespaceManager);
            if (dispenseNode == null) return;

            var quantityNode = dispenseNode.SelectSingleNode("fhir:quantity/fhir:value/@value", _namespaceManager);
            if (quantityNode != null && decimal.TryParse(quantityNode.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var quantity))
            {
                dispenseRequest.Quantity = quantity;
            }

            var unitNode = dispenseNode.SelectSingleNode("fhir:quantity/fhir:code/@value", _namespaceManager);
            dispenseRequest.Unit = unitNode?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extracts substitution information from medication request
        /// </summary>
        private void ExtractSubstitutionInfo(XmlNode requestNode, SubstitutionInfo substitution)
        {
            var substitutionNode = requestNode.SelectSingleNode("fhir:substitution/fhir:allowedBoolean/@value", _namespaceManager);
            if (substitutionNode != null && bool.TryParse(substitutionNode.Value, out var allowed))
            {
                substitution.Allowed = allowed;
            }
        }

        /// <summary>
        /// Helper method to extract address information from an address node
        /// </summary>
        private void ExtractAddress(XmlNode addressNode, AddressInfo address)
        {
            var streetNode = addressNode.SelectSingleNode("fhir:line/fhir:extension[@url='http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-streetName']/fhir:valueString/@value", _namespaceManager);
            address.Street = streetNode?.Value ?? string.Empty;

            var houseNumberNode = addressNode.SelectSingleNode("fhir:line/fhir:extension[@url='http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-houseNumber']/fhir:valueString/@value", _namespaceManager);
            address.HouseNumber = houseNumberNode?.Value ?? string.Empty;

            var cityNode = addressNode.SelectSingleNode("fhir:city/@value", _namespaceManager);
            address.City = cityNode?.Value ?? string.Empty;

            var postalCodeNode = addressNode.SelectSingleNode("fhir:postalCode/@value", _namespaceManager);
            address.PostalCode = postalCodeNode?.Value ?? string.Empty;

            var countryNode = addressNode.SelectSingleNode("fhir:country/@value", _namespaceManager);
            address.Country = countryNode?.Value ?? string.Empty;
        }
    }
}
