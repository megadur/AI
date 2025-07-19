namespace ERezeptVerordnungExtractor.Models
{
    /// <summary>
    /// Represents the extracted data from an eRezept Verordnung (prescription) FHIR Bundle
    /// </summary>
    public class ERezeptVerordnungData
    {
        public string BundleId { get; set; } = string.Empty;
        public string PrescriptionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public PractitionerInfo Practitioner { get; set; } = new();
        public OrganizationInfo Organization { get; set; } = new();
        public PatientInfo Patient { get; set; } = new();
        public CoverageInfo Coverage { get; set; } = new();
        public MedicationInfo Medication { get; set; } = new();
        public MedicationRequestInfo MedicationRequest { get; set; } = new();
    }

    /// <summary>
    /// Practitioner (doctor) information
    /// </summary>
    public class PractitionerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string LANR { get; set; } = string.Empty;
        public PractitionerNameInfo Name { get; set; } = new();
        public List<QualificationInfo> Qualifications { get; set; } = new();
    }

    /// <summary>
    /// Practitioner name information
    /// </summary>
    public class PractitionerNameInfo
    {
        public string Family { get; set; } = string.Empty;
        public string Given { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string FullName => $"{Prefix} {Given} {Family}".Trim();
    }

    /// <summary>
    /// Qualification information for practitioners
    /// </summary>
    public class QualificationInfo
    {
        public string TypeCode { get; set; } = string.Empty;
        public string TypeDisplay { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Organization (practice/clinic) information
    /// </summary>
    public class OrganizationInfo
    {
        public string Id { get; set; } = string.Empty;
        public string BSNR { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<TelecomInfo> Telecoms { get; set; } = new();
        public AddressInfo Address { get; set; } = new();
    }

    /// <summary>
    /// Telecom information (phone, fax, email)
    /// </summary>
    public class TelecomInfo
    {
        public string System { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Address information
    /// </summary>
    public class AddressInfo
    {
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string FullAddress => $"{Street} {HouseNumber}, {PostalCode} {City}, {Country}";
    }

    /// <summary>
    /// Patient information
    /// </summary>
    public class PatientInfo
    {
        public string Id { get; set; } = string.Empty;
        public string InsuranceNumber { get; set; } = string.Empty;
        public PatientNameInfo Name { get; set; } = new();
        public DateTime? BirthDate { get; set; }
        public AddressInfo Address { get; set; } = new();
    }

    /// <summary>
    /// Patient name information
    /// </summary>
    public class PatientNameInfo
    {
        public string Family { get; set; } = string.Empty;
        public string Given { get; set; } = string.Empty;
        public string FullName => $"{Given} {Family}".Trim();
    }

    /// <summary>
    /// Coverage (insurance) information
    /// </summary>
    public class CoverageInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string PayorIK { get; set; } = string.Empty;
        public string AlternativeIK { get; set; } = string.Empty;
        public string PayorName { get; set; } = string.Empty;
        public string PersonenGruppe { get; set; } = string.Empty;
        public string DmpKennzeichen { get; set; } = string.Empty;
        public string VersichertenArt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Medication information
    /// </summary>
    public class MedicationInfo
    {
        public string Id { get; set; } = string.Empty;
        public string PZN { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string NormSize { get; set; } = string.Empty;
        public bool IsVaccine { get; set; }
        public string MedicationType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Medication request (prescription) information
    /// </summary>
    public class MedicationRequestInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public DateTime? AuthoredOn { get; set; }
        public string StatusCoPayment { get; set; } = string.Empty;
        public bool EmergencyServicesFee { get; set; }
        public bool BVG { get; set; }
        public AccidentInfo Accident { get; set; } = new();
        public MultipleInfo Multiple { get; set; } = new();
        public DosageInfo Dosage { get; set; } = new();
        public DispenseRequestInfo DispenseRequest { get; set; } = new();
        public SubstitutionInfo Substitution { get; set; } = new();
    }

    /// <summary>
    /// Accident information
    /// </summary>
    public class AccidentInfo
    {
        public string Kennzeichen { get; set; } = string.Empty;
        public string Betrieb { get; set; } = string.Empty;
        public DateTime? Unfalltag { get; set; }
    }

    /// <summary>
    /// Multiple prescription information
    /// </summary>
    public class MultipleInfo
    {
        public bool Kennzeichen { get; set; }
        public int Numerator { get; set; }
        public int Denominator { get; set; }
        public DateTime? ValidityPeriodStart { get; set; }
        public DateTime? ValidityPeriodEnd { get; set; }
    }

    /// <summary>
    /// Dosage information
    /// </summary>
    public class DosageInfo
    {
        public bool DosageFlag { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dispense request information
    /// </summary>
    public class DispenseRequestInfo
    {
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Substitution information
    /// </summary>
    public class SubstitutionInfo
    {
        public bool Allowed { get; set; }
    }
}
