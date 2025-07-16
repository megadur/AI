# ERezeptExtractor

A comprehensive C# library for processing German electronic prescriptions (eRezept) and generating TA7 FHIR reports according to AVD specifications and GKVSV standards.

## Features

### 📋 eRezept Processing
- **Data Extraction**: Extract prescription data from German eRezept FHIR XML files
- **Validation**: Comprehensive validation of prescription data
- **JSON Export**: Serialize extracted data to JSON format
- **AVD Compliance**: Follows AVD specification v1.8

### 👨‍⚕️ KBV Practitioner Data
- **Practitioner Information**: Extract and validate practitioner data according to KBV requirements
- **LANR Logic**: Specialized logic for Lebenslange Arztnummer (LANR) processing
- **CSV Integration**: Import requirements from CSV files

### 🧾 TA7 FHIR Reports
- **Report Generation**: Create TA7 FHIR reports for electronic prescription billing
- **GKVSV Compliance**: Follows GKVSV standards for health insurance billing
- **XML Generation**: Generate compliant FHIR XML documents
- **Validation**: Comprehensive validation with business rules

## Installation

```bash
dotnet add package ERezeptExtractor
```

## Quick Start

### Extract eRezept Data

```csharp
using ERezeptExtractor;

var extractor = new ERezeptExtractor();
var xmlContent = File.ReadAllText("erezept.xml");

// Extract all prescription data
var prescriptions = extractor.ExtractFromXml(xmlContent);

// Export to JSON
var serializer = new ERezeptSerializer();
var json = serializer.SerializeToJson(prescriptions);
```

### Generate TA7 Report

```csharp
using ERezeptExtractor.TA7;

var generator = new TA7FhirReportGenerator();

// Create a sample report
var report = generator.CreateSampleReport();

// Generate XML
var xml = generator.GenerateTA7Report(report);

// Validate the report
var validator = new TA7ReportValidator();
var errors = validator.ValidateReport(report);
```

### Process KBV Practitioner Data

```csharp
using ERezeptExtractor.KBV;

var extractor = new KBVPractitionerExtractor();
var xmlContent = File.ReadAllText("prescription.xml");

// Extract practitioner data
var practitionerData = extractor.ExtractKBVData(xmlContent);

// Validate the data
var validator = new KBVPractitionerValidator();
var errors = validator.ValidatePractitionerData(practitionerData);
```

// Save to JSON
ERezeptSerializer.SaveToJsonFile(data, "output.json");
```

### Data Structure

The library extracts data into the following main structures:

#### ERezeptData
- Bundle ID and Prescription ID
- Timestamp
- Pharmacy information
- Medication dispense information
- Invoice information

#### PharmacyInfo
- ID and IK number
- Name and address details

#### MedicationDispenseInfo
- Dispense status and type
- Prescription reference
- Handed over date

#### InvoiceInfo
- Invoice status and totals
- Line items with PZN codes
- Copayment information
- Zusatzattribute for FAM compliance

### Validation

The library includes comprehensive validation:

```csharp
// Validate extracted data
var errors = ERezeptValidator.Validate(data);

// Validate specific fields
bool isPZNValid = ERezeptValidator.IsValidPZN("05454378");
bool isIKValid = ERezeptValidator.IsValidIKNumber("123456789");
```

### JSON Serialization

```csharp
// Convert to JSON
string json = ERezeptSerializer.ToJson(data);

// Load from JSON
var loadedData = ERezeptSerializer.FromJson(json);

// Save/Load files
ERezeptSerializer.SaveToJsonFile(data, "output.json");
var fileData = ERezeptSerializer.LoadFromJsonFile("output.json");
```

## Data Mapping

The library maps the following FHIR elements:

### Bundle Level
- `Bundle.id` → BundleId
- `Bundle.identifier.value` → PrescriptionId
- `Bundle.timestamp` → Timestamp

### Organization (Pharmacy)
- `Organization.id` → Pharmacy.Id
- `Organization.identifier.value` → Pharmacy.IK_Number
- `Organization.name` → Pharmacy.Name
- `Organization.address` → Pharmacy.Address

### MedicationDispense
- `MedicationDispense.id` → MedicationDispense.Id
- `MedicationDispense.status` → MedicationDispense.Status
- `MedicationDispense.whenHandedOver` → MedicationDispense.HandedOverDate
- `MedicationDispense.authorizingPrescription.identifier.value` → MedicationDispense.PrescriptionId

### Invoice
- `Invoice.id` → Invoice.Id
- `Invoice.status` → Invoice.Status
- `Invoice.totalGross.value` → Invoice.TotalGross
- `Invoice.totalGross.currency` → Invoice.Currency
- `Invoice.lineItem` → Invoice.LineItems

### Line Items
- `lineItem.sequence` → LineItem.Sequence
- `lineItem.chargeItemCodeableConcept.coding.code` → LineItem.PZN
- `lineItem.priceComponent.amount` → LineItem.Amount
- Extensions for VAT, copayment, and Zusatzattribute

## Zusatzattribute Support

The library fully supports the extraction of Zusatzattribute including:

- **Markt**: Market-related attributes
- **Rabattvertragserfüllung**: Discount contract fulfillment
- **PreisguenstigesFAM**: Price-favorable pharmaceuticals
- **ImportFAM**: Import pharmaceuticals

Each contains Gruppe (group) and Schlüssel (key) values according to the AVD specification.

## Error Handling

The library provides comprehensive error handling:

- File not found exceptions
- XML parsing errors
- Validation errors with detailed messages
- Missing data element handling

## Requirements

- .NET 8.0 or higher
- System.Xml.XPath
- Newtonsoft.Json

## Example Output

The extracted data can be serialized to JSON for further processing:

```json
{
  "BundleId": "72bd741c-7ad8-41d8-97c3-9aabbdd0f5b4",
  "PrescriptionId": "160.123.456.789.123.58",
  "Timestamp": "2020-02-04T15:30:00Z",
  "Pharmacy": {
    "Id": "11ba8a7b-79f6-4b7a-8a29-0524c9e0ba41",
    "IK_Number": "123456789",
    "Name": "Adler-Apotheke",
    "Address": {
      "Street": "Taunusstraße",
      "HouseNumber": "89",
      "City": "Langen",
      "PostalCode": "63225",
      "Country": "DE"
    }
  },
  "Invoice": {
    "TotalGross": 27.69,
    "Currency": "EUR",
    "Copayment": 5.00,
    "LineItems": [
      {
        "Sequence": 1,
        "PZN": "05454378",
        "Amount": 27.69,
        "VatRate": 19.0,
        "CopaymentAmount": 5.00
      }
    ]
  }
}
```

## License

This project is licensed under the MIT License.
