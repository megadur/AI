namespace ERezeptAbgabeExtractor.Models
{
    /// <summary>
    /// Represents the extracted data from an eRezept FHIR Bundle
    /// </summary>
    public class ERezeptAbgabeData
    {
        public string BundleId { get; set; } = string.Empty;
        public string PrescriptionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public PharmacyInfo Pharmacy { get; set; } = new();
        public MedicationDispenseInfo MedicationDispense { get; set; } = new();
        public InvoiceInfo Invoice { get; set; } = new();
    }

    /// <summary>
    /// Pharmacy information
    /// </summary>
    public class PharmacyInfo
    {
        public string Id { get; set; } = string.Empty;
        public string IK_Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AddressInfo Address { get; set; } = new();
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
    /// Medication dispensing information
    /// </summary>
    public class MedicationDispenseInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PrescriptionId { get; set; } = string.Empty;
        public DateTime? HandedOverDate { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Invoice/billing information
    /// </summary>
    public class InvoiceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<LineItemInfo> LineItems { get; set; } = new();
        public decimal TotalGross { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Copayment { get; set; }
    }

    /// <summary>
    /// Invoice line item information
    /// </summary>
    public class LineItemInfo
    {
        public int Sequence { get; set; }
        public string PZN { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal VatRate { get; set; }
        public decimal CopaymentAmount { get; set; }
        public string CopaymentCategory { get; set; } = string.Empty;
        public ZusatzattributeInfo Zusatzattribute { get; set; } = new();
    }

    /// <summary>
    /// Additional attributes (Zusatzattribute) information
    /// </summary>
    public class ZusatzattributeInfo
    {
        public MarktInfo Markt { get; set; } = new();
        public RabattvertragserfuellungInfo Rabattvertragserfuellung { get; set; } = new();
        public PreisguenstigesFAMInfo PreisguenstigesFAM { get; set; } = new();
        public ImportFAMInfo ImportFAM { get; set; } = new();
    }

    public class MarktInfo
    {
        public string Schluessel { get; set; } = string.Empty;
        public string Gruppe { get; set; } = string.Empty;
    }

    public class RabattvertragserfuellungInfo
    {
        public string Gruppe { get; set; } = string.Empty;
        public string Schluessel { get; set; } = string.Empty;
    }

    public class PreisguenstigesFAMInfo
    {
        public string Gruppe { get; set; } = string.Empty;
        public string Schluessel { get; set; } = string.Empty;
    }

    public class ImportFAMInfo
    {
        public string Gruppe { get; set; } = string.Empty;
        public string Schluessel { get; set; } = string.Empty;
    }
}
