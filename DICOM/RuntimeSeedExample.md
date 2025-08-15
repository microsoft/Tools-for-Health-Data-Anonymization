# DICOM Runtime Seed Parameters Example

This document demonstrates how to use the new runtime seed parameters functionality in the DICOM Anonymizer.

## Overview

The DICOM Anonymizer now supports specifying seed values as optional parameters when de-identifying a specific dataset, allowing you to override the configuration-based seeds at runtime.

## Usage Example

```csharp
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;

// Create your DICOM dataset
var dataset = new DicomDataset
{
    { DicomTag.PatientName, "John Doe" },
    { DicomTag.PatientBirthDate, "19800101" },
    { DicomTag.StudyInstanceUID, "1.2.3.4.5.6" },
};

// Initialize the anonymizer engine with your configuration
var engine = new AnonymizerEngine("configuration.json");

// Option 1: Use configuration-based seeds (existing behavior)
engine.AnonymizeDataset(dataset);

// Option 2: Use runtime seeds to override configuration
var runtimeSeeds = new RuntimeSeedSettings
{
    CryptoHashKey = "my-runtime-crypto-seed",
    DateShiftKey = "my-runtime-date-seed",
    EncryptKey = "my-runtime-encrypt-key-1234567890123456", // Must be 16, 24, or 32 bytes
};

engine.AnonymizeDataset(dataset, runtimeSeeds);
```

## Supported Runtime Seeds

- **CryptoHashKey**: Overrides the cryptographic hash seed for `cryptoHash` anonymization method
- **DateShiftKey**: Overrides the date shift seed for `dateShift` anonymization method  
- **EncryptKey**: Overrides the encryption key for `encrypt` anonymization method

## Benefits

1. **Per-dataset anonymization**: Different datasets can use different seeds while sharing the same configuration
2. **Dynamic seed generation**: Seeds can be generated programmatically based on external factors
3. **Enhanced security**: Seeds can be managed separately from configuration files
4. **Backward compatibility**: Existing code continues to work unchanged

## Configuration Example

Your configuration file can still contain default seeds:

```json
{
  "rules": [
    {"tag": "(0010,0010)", "method": "cryptoHash"},
    {"tag": "(0010,0030)", "method": "dateShift"}
  ],
  "defaultSettings": {
    "cryptoHash": {
      "cryptoHashKey": "default-crypto-seed"
    },
    "dateShift": {
      "dateShiftKey": "default-date-seed",
      "dateShiftRange": 50
    }
  }
}
```

When runtime seeds are provided, they take precedence over the configuration defaults.