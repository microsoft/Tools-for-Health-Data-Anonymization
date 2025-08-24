# DICOM Data Anonymization: A Developer-Friendly Guide

> 📝 **Note**: This is a first draft of our new developer-friendly documentation. We're actively improving it based on community feedback!

## What Problem Does This Solve? 🤔

Imagine you're building an AI model for detecting lung conditions or creating a medical education platform. You have thousands of X-rays, CT scans, and MRIs, but can't use them because they contain:
- Patient names burned into the images
- Patient IDs in metadata tags
- Hospital names and addresses
- Referring physician information
- Exact dates and times of scans

**This is where DICOM Anonymization comes in!** It helps you clean medical images for:
- 🧠 AI/ML model training
- 📚 Medical education materials
- 🔬 Research publications
- 🏥 Multi-center studies

> **📋 Have clinical records instead?** → [Go to FHIR Guide](./fhir-getting-started.md)

## Understanding the Basics First 🏗️

### What Makes DICOM Special?
DICOM files are like icebergs - what you see (the image) is just the tip:
- **Visible part**: The medical image pixels
- **Hidden part**: 2000+ metadata tags with patient/study info

### Quick Example
```
Before anonymization:
(0010,0010) Patient Name: SMITH^JOHN^A
(0010,0020) Patient ID: MRN123456
(0008,0080) Institution: Seattle General Hospital
(0008,1048) Physician: Dr. Jane Wilson

After anonymization:
(0010,0010) Patient Name: ANON12345
(0010,0020) Patient ID: [REMOVED]
(0008,0080) Institution: [REMOVED]
(0008,1048) Physician: [REMOVED]
```

## Your First Anonymization in 5 Minutes ⚡

### Prerequisites
- .NET 6.0+ ([download](https://dotnet.microsoft.com/download))
- Git
- Sample DICOM files (.dcm)

### Let's Go!
```bash
# 1. Get and build the tool
git clone https://github.com/microsoft/Tools-for-Health-Data-Anonymization.git
cd Tools-for-Health-Data-Anonymization
dotnet build

# 2. Prepare your DICOM files
mkdir my-dicoms
# Copy your .dcm files here

# 3. Navigate to the built tool
cd DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/bin/Debug/net6.0

# 4. Run anonymization
# Windows:
./Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i my-dicoms -o anonymized-dicoms

# macOS/Linux:
dotnet Microsoft.Health.Dicom.Anonymizer.CommandLineTool.dll -i my-dicoms -o anonymized-dicoms

# That's it! Your anonymized DICOMs are ready
```

## Key Anonymization Features 🔧

| Feature | What It Does | Example |
|---------|--------------|---------|
| **Tag Removal** | Deletes sensitive metadata | Patient Name → [blank] |
| **Tag Replacement** | Substitutes with safe values | Patient ID → ANON001 |
| **Date Shifting** | Preserves temporal relationships | 2024-01-15 → 2024-02-03 |
| **UID Remapping** | Maintains study relationships | Consistent new UIDs |

## Common Use Cases 🎯

### AI Training Dataset
Save as `config-ai-training.json`:
```json
{
  "rules": [
    {"tag": "(0010,0010)", "method": "replace", "value": "TRAINING_CASE"},
    {"tag": "(0010,0020)", "method": "hash"},
    {"tag": "(0008,0080)", "method": "remove"}
  ]
}
```

Then use it:
```bash
./Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i input -o output -c config-ai-training.json
```

### Public Education Material
```json
{
  "rules": [
    {"tag": "(0010,xxxx)", "method": "remove"},  // All patient tags
    {"tag": "(0008,0080)", "method": "replace", "value": "Teaching Hospital"},
    {"tag": "(0008,0090)", "method": "remove"}   // Referring physician
  ]
}
```

## Working with Different Scenarios 📁

### Single File
```bash
./Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i scan.dcm -o anonymized_scan.dcm
```

### Entire Study/Series
```bash
./Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i /path/to/study -o /path/to/output -r
```

### Batch Processing
```bash
# Windows batch example
for /D %d in (.\studies\*) do (
  Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i "%d" -o ".\anonymized\%~nd"
)

# Linux/Mac bash example
for dir in ./studies/*; do
  dotnet Microsoft.Health.Dicom.Anonymizer.CommandLineTool.dll -i "$dir" -o "./anonymized/$(basename $dir)"
done
```

## Important DICOM Considerations ⚠️

### Burned-in Annotations
Some images have patient info "burned" into the pixels themselves:

![Burned-in annotation example](./images/burned-in-example.png)

**Current limitation**: This tool handles metadata only. For burned-in text, consider:
- Manual review and redaction
- Computer vision approaches
- Requesting clean exports from imaging systems

### Preserving Study Integrity
When anonymizing related images:
- Process entire studies together
- Use consistent configuration
- Maintain series relationships

## Quick Configuration Examples 🎯

### Minimal Anonymization (Keep Study Structure)
```json
{
  "keepTags": ["StudyInstanceUID", "SeriesInstanceUID"],
  "removeTags": ["PatientName", "PatientID"],
  "hashTags": ["AccessionNumber"]
}
```

### Maximum Privacy (Research Use)
```json
{
  "removeAllPrivateTags": true,
  "removeTags": ["group:0010", "group:0008"],
  "keepTags": ["Modality", "StudyDate"]
}
```

## Next Steps 📚

- **Need tag details?** → [DICOM Tag Reference](../DICOM-anonymization.md#dicom-tag-reference)
- **Custom rules?** → [Advanced Configuration](../DICOM-anonymization.md#configuration-guide)
- **Bulk processing?** → [Azure Integration](../DICOM-anonymization.md#azure-setup)

---

> 💡 **Tip**: Start with a single file to test your configuration, then scale up!

> ⚠️ **Remember**: Always verify anonymized images are truly de-identified before sharing!
