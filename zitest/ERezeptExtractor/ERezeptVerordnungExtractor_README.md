# ERezeptVerordnungExtractor

This document describes the newly created `ERezeptVerordnungExtractor` that extracts prescription data (Verordnungsdaten) from FHIR XML bundles.

## Overview

The `ERezeptVerordnungExtractor` is designed similar to the existing `ERezeptAbgabeExtractor` but specifically handles prescription data from FHIR bundles containing:

- Practitioner (Doctor) information
- Organization (Practice/Clinic) information
- Patient information
- Coverage (Insurance) information
- Medication information
- MedicationRequest (Prescription details) information

## Files Created

1. **Models/ERezeptVerordnungModels.cs** - Data models for prescription data
2. **ERezeptVerordnungExtractor.cs** - Main extractor class
3. **Tests/ERezeptVerordnungExtractorTests.cs** - Unit tests
4. **Demo/VerordnungDemo.cs** - Demo program showing usage

## Key Features

### Data Models
- `ERezeptVerordnungData` - Main container for all extracted data
- `PractitionerInfo` - Doctor information (LANR, name, qualifications)
- `OrganizationInfo` - Practice/clinic information (BSNR, name, address, contact)
- `PatientInfo` - Patient information (insurance number, name, birthdate, address)
- `CoverageInfo` - Insurance information (type, payor, special designations)
- `MedicationInfo` - Medication details (PZN, name, form, category)
- `MedicationRequestInfo` - Prescription details (dosage, dispense request, accident info)

### Extractor Features
- Extracts from XML string, file path, or XmlDocument
- Comprehensive data extraction from all FHIR bundle sections
- Proper handling of FHIR namespaces and XPath queries
- Support for complex nested structures (accident info, multiple prescriptions, etc.)
- Error handling for missing or invalid data

### Sample Extracted Data

From the provided `bundle_Verordnungsdaten.xml`:

```
Bundle Information:
  Bundle ID: 0f1be812-21cc-45a9-a4b0-0df2ad9a15b4
  Prescription ID: 160.000.200.744.243.73
  Timestamp: 2024-03-01 09:48:32

Practitioner (Doctor):
  LANR: 777426306
  Name: Dr.med. Ralf Greese
  Qualifications: Arzt (00), FA Chirurgie

Organization (Practice):
  BSNR: 830164500
  Name: Dr. Ralf Greese
  Address: Meyenburger Chaussee 23, 16909 Wittstock, D
  Contact: Phone, Fax, Email

Patient:
  Insurance Number: M415602272
  Name: Keven Friske
  Birth Date: 1990-02-03
  Address: Heinrichsdorfer Str. 11, 16909 Wittstock, D

Coverage (Insurance):
  Type: BG (Berufsgenossenschaft)
  Payor: BG Nahrungsmittel und Gaststätten BV Mannheim
  IK Number: 120890837

Medication:
  PZN: 06313409
  Name: Ibuflam 600mg Lichtenstein
  Form: FTA (Filmtablette)
  Norm Size: N2

Prescription Details:
  Status: active
  Dosage: >>3 x 1<<
  Quantity: 1.0 {Package}
  Substitution Allowed: true
  Accident: Fresh Fruits GmbH on 2024-02-29
```

## Usage Example

```csharp
using ERezeptVerordnungExtractor;

var extractor = new ERezeptVerordnungExtractor();
var data = extractor.ExtractFromFile("bundle_Verordnungsdaten.xml");

Console.WriteLine($"Doctor: {data.Practitioner.Name.FullName}");
Console.WriteLine($"Patient: {data.Patient.Name.FullName}");
Console.WriteLine($"Medication: {data.Medication.Name}");
Console.WriteLine($"Dosage: {data.MedicationRequest.Dosage.Text}");
```

## Key Differences from ERezeptAbgabeExtractor

1. **Focus on Prescription Data**: Extracts prescribing information rather than dispensing information
2. **Practitioner Details**: Comprehensive extraction of doctor information including LANR and qualifications
3. **Organization Information**: Practice/clinic details with BSNR
4. **Patient Information**: Complete patient demographics
5. **Prescription Details**: MedicationRequest with dosage, accident information, etc.

## Implementation Notes

The extractor uses XPath queries with FHIR namespace management to navigate the complex XML structure. It properly handles:

- Optional elements that may not be present
- Complex nested structures with extensions
- Date/time parsing with proper error handling
- Boolean value extraction
- Decimal number parsing for quantities

## Testing

The implementation includes comprehensive unit tests covering:
- Basic extraction functionality
- Individual component extraction (practitioner, patient, medication, etc.)
- Error handling for invalid inputs
- Edge cases and optional fields

The extractor is designed to be robust and handle real-world FHIR bundles with varying structures and optional elements.
