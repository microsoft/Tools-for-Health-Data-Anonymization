# FHIR Data Anonymization: A Developer-Friendly Guide

> 📝 **Note**: This is a first draft of our new developer-friendly documentation. We're actively improving it based on community feedback!

## What Problem Does This Solve? 🤔

Imagine you're building a healthcare analytics platform. You have access to thousands of patient records with valuable insights, but you can't use them because they contain sensitive information like:
- Patient names and addresses
- Social security numbers
- Exact dates of procedures
- Phone numbers and email addresses

**This is where FHIR Data Anonymization comes in!** It helps you transform real patient data into anonymized datasets that are safe to use for:
- 📊 Research and analytics
- 🧪 Testing and development
- 📈 Public health studies
- 🎓 Educational purposes

> **🏥 Have medical images instead?** → [Go to DICOM Guide](./dicom-getting-started.md)

## Understanding the Basics First 🏗️

### What is Data Anonymization?
Think of it like creating a "stunt double" for your data. The anonymized data looks and behaves like the original, but all identifying information is either:
- **Removed** (like deleting names)
- **Transformed** (like shifting dates by random amounts)
- **Generalized** (like replacing exact age "42" with range "40-45")

### Quick Example
```json
// Before anonymization
{
  "resourceType": "Patient",
  "name": [{"given": ["John"], "family": "Smith"}],
  "birthDate": "1980-05-15"
}

// After anonymization
{
  "resourceType": "Patient",
  "name": [{"given": ["REDACTED"], "family": "REDACTED"}],
  "birthDate": "1980-07-22"  // Date shifted
}
```

## Your First Anonymization in 5 Minutes ⚡

### Prerequisites
- .NET 6.0+ ([download](https://dotnet.microsoft.com/download))
- Git

### Option 1: Using Test FHIR Data 🧪

Don't have FHIR data yet? Let's grab some from a public test server:

```bash
# Get sample patient data from HAPI test server
curl -H "Accept: application/fhir+json" \
     "https://hapi.fhir.org/baseR4/Patient?_count=5" \
     -o sample-patients.json

# Or get specific resource types
curl "https://hapi.fhir.org/baseR4/Observation?_count=10" -o observations.json
curl "https://hapi.fhir.org/baseR4/Encounter?_count=10" -o encounters.json
```

**Useful Test Servers:**
- 🔗 [HAPI Test Server](https://hapi.fhir.org/) - R4, STU3, DSTU2 data
- 🔗 [SMART Launcher](https://launch.smarthealthit.org/) - Synthetic patient data
- 🔗 [Synthea Sample Data](https://synthea.mitre.org/downloads) - Realistic synthetic records

### Option 2: Using Your Own Data

```bash
# 1. Get and build the tool
git clone https://github.com/microsoft/Tools-for-Health-Data-Anonymization.git
cd Tools-for-Health-Data-Anonymization
dotnet build

# 2. Create test data or use existing
mkdir my-data
echo '{"resourceType": "Patient", "id": "123", "name": [{"given": ["Jane"]}]}' > my-data/patient.json

# 3. Run anonymization
cd FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Debug/net8.0
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe -i my-data -o output

# That's it! Check the output folder
```

## Working with Bulk Export Data 📦

Many FHIR servers support bulk export in NDJSON format:

```bash
# If you have bulk exported data (NDJSON format)
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe -i bulk-export -o anonymized -b

# The -b flag tells the tool to expect NDJSON format
```

## Key Anonymization Methods 🔧

| Method | What It Does | Example |
|--------|--------------|---------|
| `redact` | Removes completely | Names → [REMOVED] |
| `dateShift` | Shifts dates randomly | 2020-01-15 → 2020-02-03 |
| `cryptoHash` | Creates consistent hash | ID "123" → "a3c024..." |
| `generalize` | Makes less specific | Age 42 → "40-45" |

## Common Use Cases 🎯

### Test Data for Development
```json
{
  "fhirPathRules": [
    {"path": "Patient.name", "method": "substitute", "replaceWith": "Test Patient"},
    {"path": "Patient.telecom", "method": "redact"}
  ]
}
```

### Research Data
```json
{
  "fhirPathRules": [
    {"path": "Resource.id", "method": "cryptoHash"},
    {"path": "Patient.birthDate", "method": "dateShift"}
  ]
}
```

## Next Steps 📚

- **Need more control?** → [Advanced Configuration](../FHIR-anonymization.md#configuration-file-format)
- **Processing lots of data?** → [Azure Pipeline Setup](../FHIR-anonymization.md#anonymize-fhir-data-using-azure-data-factory)
- **HIPAA compliance?** → [Safe Harbor Config](../FHIR-anonymization.md#sample-configuration-file)

---

> 💡 **Tip**: Test with data from HAPI server first, then move to your production data!
