using ERezeptExtractor.Models;

namespace ERezeptExtractor.Models
{
    /// <summary>
    /// Extended model for practitioner information from KBV specifications
    /// </summary>
    public class PractitionerInfo
    {
        /// <summary>
        /// LANR (Lebenslange Arztnummer) of the prescribing person
        /// Should be "000000000" if no doctor number is available
        /// </summary>
        public string LANR { get; set; } = string.Empty;

        /// <summary>
        /// LANR of the responsible person (only filled if prescribing person is assistant)
        /// </summary>
        public string LANR_Responsible { get; set; } = string.Empty;

        /// <summary>
        /// Practitioner ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Name information
        /// </summary>
        public PractitionerNameInfo Name { get; set; } = new();

        /// <summary>
        /// Qualification information
        /// </summary>
        public List<QualificationInfo> Qualifications { get; set; } = new();

        /// <summary>
        /// Address information
        /// </summary>
        public AddressInfo Address { get; set; } = new();
    }

    /// <summary>
    /// Practitioner name information
    /// </summary>
    public class PractitionerNameInfo
    {
        public string Family { get; set; } = string.Empty;
        public string Given { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string FullName => $"{Prefix} {Given} {Family} {Suffix}".Trim();
    }

    /// <summary>
    /// Qualification information for practitioners
    /// </summary>
    public class QualificationInfo
    {
        /// <summary>
        /// Qualification type code (00=Arzt, 03=Assistenz, 04=Verantwortlicher Arzt)
        /// </summary>
        public string TypeCode { get; set; } = string.Empty;

        /// <summary>
        /// Qualification type description
        /// </summary>
        public string TypeDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Associated LANR for this qualification
        /// </summary>
        public string AssociatedLANR { get; set; } = string.Empty;
    }

    /// <summary>
    /// Extended ERezept data model including practitioner information
    /// </summary>
    public class ExtendedERezeptData : ERezeptData
    {
        /// <summary>
        /// Practitioner/doctor information
        /// </summary>
        public PractitionerInfo Practitioner { get; set; } = new();

        /// <summary>
        /// Alternative IK number from Coverage payer for accident insurance (UK) or professional associations (BG)
        /// Extracted from fhir:Coverage with payer type UK or BG
        /// </summary>
        public string KostentraegerBG { get; set; } = string.Empty;
    }
}
