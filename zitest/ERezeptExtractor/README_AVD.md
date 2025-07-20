# AVD Specification-Based FHIR Extractor

## Overview

The AVD (Abrechnungsdatenverarbeitung) Specification-Based FHIR Extractor is a comprehensive C# solution that automatically extracts data from German eRezept FHIR XML files based on standardized CSV specifications. This system intelligently parses business rules and XPath expressions to provide accurate, compliant data extraction.

## Features

### 🎯 Specification-Driven Extraction
- **CSV-Based Configuration**: Load extraction rules from standardized AVD specification files
- **Business Logic Support**: Handles complex conditional extraction rules (e.g., LANR logic)
- **Multi-Profile Support**: Works with various FHIR profiles (KBV, Gematik, ABDA)
- **Automatic Validation**: Built-in validation of extracted data against specifications

### 🔍 Advanced FHIR Processing
- **XPath-Based Extraction**: Robust XML parsing with namespace support
- **Conditional Logic**: Implements German healthcare-specific business rules
- **Fallback Strategies**: Multi-tier XPath expressions with graceful degradation
- **Data Type Conversion**: Automatic conversion between string, numeric, and date types

### 📊 Data Quality & Validation
- **Specification Validation**: Validates CSV specifications for completeness
- **Field Validation**: Length, format, and requirement validation
- **Error Reporting**: Comprehensive error and warning collection
- **Data Completeness**: Reports on missing required fields

### 🛠️ Developer-Friendly API
- **Fluent Interface**: Easy-to-use extraction API
- **Logging Integration**: Full Microsoft.Extensions.Logging support
- **Dependency Injection**: Built for modern .NET applications
- **Comprehensive Testing**: Full unit test coverage

## Architecture

```
AVDBasedExtractor (Facade)
├── AVDSpecificationParser (CSV Processing)
├── AVDFhirExtractor (FHIR XML Processing)
├── Models/
│   ├── AVDSpecification (Specification Model)
│   └── AVDExtractedData (Result Container)
└── Serialization/
    ├── AVDSpecificationParser (CSV Parser)
    └── AVDFhirExtractor (FHIR Processor)
```

## Quick Start

### 1. Installation

Add the package reference:
```xml
<PackageReference Include="ERezeptExtractor" Version="1.0.0" />
```

### 2. Basic Usage

```csharp
using ERezeptExtractor;

// Initialize the extractor
var extractor = new AVDBasedExtractor();

// Extract data from FHIR XML file using AVD specifications
var result = extractor.ExtractFromFile(
    @"path\to\fhir-bundle.xml", 
    @"path\to\AVD_Daten - Tabellenblatt2.csv"
);

// Check results
if (result.ExtractionSuccess)
{
    // Access extracted values
    var prescriptionId = result.ExtractedData.GetValue<string>("rezept_id");
    var patientName = result.ExtractedData.GetValue<string>("pat_nachname");
    var doctorLANR = result.ExtractedData.GetValue<string>("la_nr");
    
    Console.WriteLine($"Extracted {result.ExtractedData.ExtractedValues.Count} values");
}
else
{
    // Handle errors
    foreach (var error in result.ExtractedData.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### 3. Advanced Usage with Dependency Injection

```csharp
// Program.cs
var builder = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddLogging();
        services.AddScoped<AVDBasedExtractor>();
    });

var host = builder.Build();
var extractor = host.Services.GetRequiredService<AVDBasedExtractor>();
```

## Specification Format

The CSV specification format follows the AVD standard:

| Column | Description | Example |
|--------|-------------|---------|
| Attribut | Field identifier | `la_nr` |
| ID | Unique numeric ID | `42` |
| Beschreibung des Attributs | German description | `Arztnummer (LANR)` |
| Länge | Length constraint | `9`, `leer oder 9` |
| Darstellung | Data type | `alphanumerisch`, `numerisch` |
| Profile | FHIR profile URL(s) | `https://fhir.kbv.de/...` |
| Anpassung notwendig Zusätzl. | Adaptation flag | `x` |
| Partieller XPath | XPath expression(s) | `fhir:identifier/.../fhir:value` |

## Business Logic Examples

### LANR (Doctor Number) Logic

The system handles complex conditional logic such as:

```
la_nr (ID 42): Primary doctor number
- Try assistant doctor (Type 03) first
- Fall back to responsible doctor (Type 00 or 04)
- Default to "000000000" if none found

la_nr_v (ID 52): Responsible doctor number
- Only populate if assistant was found in la_nr
- Extract responsible doctor (Type 00 or 04)
- Leave empty otherwise
```

### BSNR (Practice Number) Logic

```
bs_nr (ID 61): Practice number
- Try doctor BSNR first
- Fall back to hospital IK
- Default to "0" if neither found
```

## Supported FHIR Profiles

- **KBV (Kassenärztliche Bundesvereinigung)**
  - KBV_PR_FOR_Practitioner
  - KBV_PR_FOR_Patient  
  - KBV_PR_FOR_Organization
  - KBV_PR_ERP_Prescription
  - KBV_PR_ERP_Medication_*

- **Gematik**
  - GEM_ERP_PR_Composition
  - ErxComposition

- **ABDA (Federal Association of German Pharmacists)**
  - DAV-PR-ERP-Abgabeinformationen
  - DAV-PR-ERP-Abrechnungszeilen

## API Reference

### AVDBasedExtractor

Main facade class for extraction operations.

#### Methods

```csharp
// Extract from file
AVDExtractionResult ExtractFromFile(string fhirXmlPath, string specificationCsvPath)

// Extract from XML string
AVDExtractionResult ExtractFromXml(string fhirXmlContent, string specificationCsvPath)

// Analyze specifications
Dictionary<string, List<AVDSpecification>> GetSpecificationsByProfile(string specificationCsvPath)
List<AVDSpecification> GetComplexSpecifications(string specificationCsvPath)
AVDSpecificationReport ValidateSpecifications(string specificationCsvPath)
```

### AVDExtractedData

Container for extraction results.

#### Methods

```csharp
// Type-safe value access
T GetValue<T>(string attribut)
void SetValue(string attribut, object value)

// Validation
ValidationResult ValidateData(List<AVDSpecification> specifications)
```

#### Properties

```csharp
Dictionary<string, object> ExtractedValues  // All extracted values
List<string> Errors                         // Extraction errors
List<string> Warnings                       // Extraction warnings
DateTime ExtractionTimestamp               // When extraction occurred
```

### AVDSpecification

Represents a single extraction rule.

#### Properties

```csharp
string Attribut                    // Field name (e.g., "la_nr")
int ID                            // Unique identifier
string BeschreibungDesAttributs   // German description
string Laenge                     // Length specification
string Darstellung               // Data type (numerisch/alphanumerisch)
string Profile                   // FHIR profile URL
string PartiellerXPath          // XPath expression(s)
List<string> BusinessRules      // Parsed business rules
List<string> XPathExpressions   // Parsed XPath expressions
bool IsOptional                 // Can be empty
bool RequiresAdaptation        // Needs special processing
```

## Complex Specification Examples

### Conditional Extraction (la_nr)

```csv
la_nr,42,"Arztnummer (LANR)",9,alphanumerisch,https://fhir.kbv.de/StructureDefinition/KBV_PR_FOR_Practitioner,,"fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value 
{{LANR der Assistenz (Typ 03) nutzen, falls dieser XPath einen Treffer findet}} 
fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and fhir:code/@value='03']/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value 
{{LANR des Verantwortlichen (Typ 00 oder 04) nutzen falls LANR der Assistenz (Typ 03) nicht gefunden}} 
fhir:Practitioner/fhir:qualification/fhir:code/fhir:coding[fhir:system/@value='https://fhir.kbv.de/CodeSystem/KBV_CS_FOR_Qualification_Type' and (fhir:code/@value='00' or fhir:code/@value='04')]/../../../fhir:identifier[fhir:system/@value='https://fhir.kbv.de/NamingSystem/KBV_NS_Base_ANR']/fhir:value"
```

### Multiple Profile Support

```csv
v_kategorie,81,"Verordnungskategorie",2,alphanumerisch,"https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Medication_PZN
https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Medication_Ingredient
https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Medication_Compounding
https://fhir.kbv.de/StructureDefinition/KBV_PR_ERP_Medication_FreeText",,"{{Medication}} 
fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_Medication_Category']/fhir:valueCoding/fhir:code 
{{Medication_Ingredient}} 
fhir:extension[@url='https://fhir.kbv.de/StructureDefinition/KBV_EX_ERP_Medication_Category']/fhir:valueCoding/fhir:code"
```

## Error Handling

The system provides comprehensive error handling:

### Extraction Errors
- Invalid XPath expressions
- Missing FHIR elements
- XML parsing failures
- Namespace resolution issues

### Validation Errors
- Required field violations
- Length constraint violations
- Data type mismatches
- Business rule violations

### Specification Errors
- Duplicate field IDs
- Missing XPath expressions
- Invalid CSV format
- Profile URL issues

## Performance Considerations

### Memory Usage
- Streaming CSV parsing for large specification files
- Efficient XML DOM processing
- Minimal object allocation during extraction

### Processing Speed
- Cached namespace managers
- Optimized XPath expressions
- Parallel processing for multiple specifications (future enhancement)

### Scalability
- Designed for batch processing
- Stateless extraction operations
- Thread-safe design

## Testing

The project includes comprehensive test coverage:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing  
- **Specification Tests**: CSV parsing validation
- **FHIR Tests**: XML extraction validation
- **Business Logic Tests**: Complex rule testing

## Logging

Full logging integration with Microsoft.Extensions.Logging:

```csharp
// Configure logging levels
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

### Log Categories
- `ERezeptExtractor.AVDBasedExtractor`: Main extraction workflow
- `ERezeptExtractor.Serialization.AVDSpecificationParser`: CSV parsing
- `ERezeptExtractor.Serialization.AVDFhirExtractor`: FHIR processing

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions and support:
- Create an issue on GitHub
- Check the existing documentation
- Review the unit tests for examples

## Changelog

### Version 1.0.0
- Initial release
- Complete AVD specification support
- FHIR XML extraction engine
- Business logic implementation
- Comprehensive validation
- Full test coverage
