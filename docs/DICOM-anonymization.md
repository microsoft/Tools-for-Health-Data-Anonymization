# DICOM Data Anonymization

The Digital Imaging and Communication in Medicine (DICOM) standard has been commonly used for storing, viewing, and transmitting information in medical imaging. A DICOM file not only contains a viewable image but also a header with a large variety of data elements. These meta-data elements include identifiable information about the patient, the study, and the institution. Sharing such sensitive data demands proper protection to ensure data safety and maintain patient privacy. DICOM Anomymization Tool helps anonymize metadata in DICOM files for this purpose.

### Features
- Support anonymization methods for DICOM metadata including redact, keep, encrypt, cryptoHash, dateShift, perturb, substitute, remove and refreshUID.
- Configuration of the data elements that need to be anonymized.
- Configuration of the anonymization methods for each data element.
- Ability to run the tool on premise to anonymize a dataset locally.

### Build the solution
Use the .Net Core SDK to build DICOM Anonymization Tool. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download/dotnet/6.0).

### Prepare DICOM Data
You can prepare your own DICOM files as input, or use sample DICOM files in folder $SOURCE\DICOM\samples of the project.

The tool applies configured metadata transformations; it does not inspect or
sanitize unselected payload contents. Calling applications determine which
input content types are acceptable.

### Table of Contents

- [Anonymize DICOM data: using the command line tool](#anonymize-dicom-data-using-the-command-line-tool)
- [Customize configuration file](#customize-configuration-file)
- [Data anonymization algorithms](#data-anonymization-algorithms)
- [Output validation](#output-validation)


## Anonymize DICOM data: using the command line tool

Once you have built the command line tool, you will find executable file Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe in the $SOURCE\DICOM\src\Microsoft.Health.Dicom.Anonymizer.CommandLineTool\bin\Debug|Release\net8.0 folder.

You can use this executable file to anonymize DICOM file.

```
> .\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i myInputFile -o myOutputFile
```

### Use Command Line Tool
The command-line tool can be used to anonymize one DICOM file or a folder containing DICOM files. Here are the parameters that the tool accepts:


| Option | Name | Optionality | Default | Description |
| ----- | ----- | ----- |----- |----- |
| -i | inputFile | Required (for file conversion) | | Input DICOM file. |
| -o | outputFile | Required (for file conversion) | |  Output DICOM file. |
| -c | configFile | Optional |configuration.json | Anonymizer configuration file path. It reads the default file from the current directory. |
| -I | inputFolder | Required (for folder conversion) |  | Input folder. |
| -O | outputFolder | Required (for folder conversion) |  | Output folder. |
| --validateInput | validateInput | Optional | false | Validate input DICOM file against value multiplicity, value types and format in [DICOM specification](http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part06/chapter_6.html). |
| --validateOutput | validateOutput | Optional | false | Validate output DICOM file against value multiplicity, value types and format in [DICOM specification](http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part06/chapter_6.html). |

> **[NOTE]**
> To anonymize one DICOM file, inputFile and outputFile are required. To anonymize a DICOM folder, inputFolder and outputFolder are required.

Example usage to anonymize DICOM files in a folder:
```
.\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -I myInputFolder -O myOutputFolder -c myConfigFile
```

## Sample configuration file
The configuration is specified in JSON format and has three required high-level sections. The first section named _rules_, it specifies anonymization methods for DICOM tag. The second and third sections are _defaultSettings_ and _customSettings_ which specify default settings and custom settings for anonymization methods respectively.

|Fields|Description|
|----|----|
|rules|Anonymization rules for tags.|
|defaultSettings|Default settings for anonymization functions. Default settings will be used if not specify settings in rules.|
|customSettings|Custom settings for anonymization functions.|


DICOM Anonymization tool comes with a sample configuration file to help meet the requirements of HIPAA Safe Harbor Method. DICOM standard also describes attributes within a DICOM dataset that may potentially result in leakage of individually identifiable information according to HIPAA Safe Harbor. Our tool will build in a sample [configuration file](../DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/configuration.json) that covers [application level confidentiality profile attributes](http://dicom.nema.org/medical/dicom/2018e/output/chtml/part15/chapter_E.html) defined in DICOM standard.

## Customize configuration file

### How to set rules

Users can list anonymization rules for individual DICOM tag (by tag value or tag name) as well as a set of tags (by masked value or DICOM VR). Ex：
```
{
    "rules": [
            {"tag": "(0010,1010)","method": "perturb"}, 
            {"tag": "(0040,xxxx)",  "method": "redact"},
            {"tag": "PatientID",  "method": "cryptohash"},
            {"tag": "PN", "method": "encrypt"}
    ]
}
```
Parameters in each rule:

|Fields|Description| Valid Value|Required|default value|
|--|-----|-----|--|--|
|tag|Used to define DICOM elements |1. Tag Value, e.g. (0010, 0010) or 0010,0010 or 00100010. <br>2. Tag Name. e.g. PatientName. <br> 3. Masked DICOM Tag (see note) <br> 4. DICOM VR. e.g. PN, DA.|True|null| 
|method|anonymization method| keep, redact, perturb, dateshift, encrypt, cryptohash, substitute, refreshUID, remove.| True|null|
|setting| Setting for anonymization method. Users can add custom settings in the field of "customSettings" and specify setting's name here. |valid setting's name |False|Default setting in the field of "defaultSettings"|
|params|parameters override setting for anonymization methods.|valid parameters|False|null|

> Masked tags follow the [DICOM convention](https://dicom.nema.org/medical/dicom/current/output/chtml/part06/chapter_5.html). `x` in a group or element number, means any value from 0 through F inclusive.

Rules retain first-match behavior, including duplicate exact selectors and
overlapping exact, masked-tag, or VR selectors. A later rule does not override
an earlier rule that has already handled the same element.

Configuration loading retains the repository's JSON deserialization behavior:
annotation fields are not subject to a strict field whitelist, and duplicate
JSON properties use the serializer's existing handling. Rule fields, tag and
method parsing, and required processor settings still have their ordinary error
checks; malformed JSON and unknown processors are not silently accepted.
Settings are copied before per-rule overrides are applied, so one rule's
parameters do not change another rule's defaults. Updated parsing diagnostics
do not include setting values or value-bearing inner exceptions.

Constructing an engine is not a guarantee that every dataset can be processed.
Selectors are not rejected solely because they are broad, masked, duplicated,
unknown to the tag dictionary, or associated with multiple possible VRs.
Processing checks the actual selected items and retains the input/output
safeguards described below.

### UID safety and compatibility notes

- The command-line tool validates File Meta SOP Class/Instance identities against
  their Dataset counterparts before processing. After a Dataset SOP Instance UID is
  refreshed, `MediaStorageSOPInstanceUID` is synchronized before output. Missing or
  stale identities fail closed with an error code and tags only; UID values and
  filenames are not included. File processing uses a copy, so failure does not
  mutate the caller's file or write a partial output. The returned file owns its
  data buffers and remains readable and saveable after anonymization completes.
- SOP Class UID, Media Storage SOP Class UID, Transfer Syntax UID, Referenced SOP
  Class UID, and SOP Classes in Study are invariants.
  If a selected operation would transform an actual invariant element, processing
  fails with `UnsupportedAnonymizationMethod` (`1101`) before applying rules.
  An earlier matching `keep` can preserve an invariant while a later broad rule
  handles compatible instance UIDs. SOP Instance and reference UIDs can still use
  `refreshUID` and retain consistent remapping.
- Exact `refreshUID` tag rules also apply to matching nested instance/reference
  UIDs. The existing UID map is shared in-process; these changes do not provide
  job or tenant isolation.
- Sequence nesting is limited to 64 item levels; deeper input fails before
  anonymization begins. Cyclic in-memory graphs are also rejected by this depth
  check before recursive validation or processing.

Broad `UI` and `SQ` rules can be configured. Their effect depends on the actual
input and first matching rule: a compatible UID can be refreshed, and a sequence
can be removed or emptied. These operations do not imply that arbitrary inputs,
invariant UIDs, or required sequence structures can safely be transformed.

### Selected nested rules

Exact tag rules also apply inside sequences for supported string `cryptoHash`,
non-invariant `UI` `refreshUID`, and scalar `remove`/`redact`. Exact nested
`substitute` requires a single-valued string element and a nonempty replacement
that validates for its actual VR. Hashing retains VR and value multiplicity.
Removal deliberately removes an element; redaction deliberately clears or
partially redacts it using its settings. Neither is a substitute for a policy
that must retain a required nonempty value.

Rule ordering still applies to each element. An earlier matching `keep` prevents
a later transformation of that element. Selected rules execute in declared order
within each dataset, including dependencies between private data and its creator.
Actual-item preflight supplies custom selectors with that dataset's identifiers,
the supplied runtime keys, and prospective visitation by earlier rules. It does
not apply processors, mutate input values, or simulate processor side effects.
Keeping a sequence retains its structure
but does not exempt its descendants from selected exact rules. A selected exact
sequence `remove` discards its subtree; sequence `redact` empties its items.
Descendant actions do not resurrect that discarded content.
No additional descendant transformations are required for a sequence selected
for removal or emptying. This does not implicitly discard other sequences.

Nested rule resolution is limited to actual exact-tag candidates. An absent
creator-bound private selector does not match unrelated nested data, and custom
selectors are not invoked there solely to inspect noncandidate elements.
Unselected sequence containers are still traversed to find targeted descendants.

Masked-tag and VR-wide transformations are not newly applied recursively.
An exact nested candidate whose first matching non-keep rule cannot safely run
there is rejected with `UnsupportedAnonymizationMethod` (`1101`) before root
processing. This includes exact nested `dateShift`, encryption, perturbation,
custom processors, unsupported bulk shapes, and invalid nested substitutions.
Previously such exact rules could be silently skipped inside sequences; this
new rejection is an intentional compatibility change. Actual root method/item
compatibility is also checked before processing.

These checks do not infer IOD-specific Type 1/1C requirements or certify clinical
conformance. Policies must choose valid nonempty substitutions when required and
must not remove required sequence structures.

### File ownership and streaming

`AnonymizeFile` returns an independently owned copy and leaves the supplied file
unchanged on failure. Its copy is materialized in memory.

`AnonymizeFileInPlace(DicomFile, RuntimeKeySettings)` (or its overload without
runtime keys) validates and synchronizes File Meta identity without cloning,
serializing, or reopening the file. Kept bulk buffers are not replaced or read
by metadata processing. The caller retains ownership of the file and any source
stream and must keep that stream open until saving is complete.

Preflight rejection does not apply anonymization rules. A later processor or
output-validation failure can leave an in-place file partially modified: discard
it and do not save or publish it. This API does not provide rollback, atomic
saves, or transactional folder output.

### How to set settings
_defaultSettings_ and _customSettings_ are used to config anonymization method. (Detailed parameters are defined in [Anonymization algorithm](#data-anonymization-algorithms). _defaultSettings_ are used when user does not specify settings in rule. As for _customSettings_, users need to add the setting with unique name. This setting can be used in "rules" by name.

Here is an example, the first rule will use `perturb` setting in _defaultSettings_ and the second one will use `perturbCustomerSetting` in field _cutomSettings_.

```
{
    "rules": [
        {"tag": "(0010,0020)","method": "perturb"},
        {"tag": "(0010,1010)","method": "perturb", "setting":"perturbCustomSetting"}
    ],
    "defaultSettings":[
        {"perturb":{ "span": "1", "roundTo": 2, "rangeType": "Proportional"}}
    ],
    "customSettings":[
        {"perturbCustomSetting":{ "span": "10", "roundTo": 2, "rangeType": "Fixed"}}
    ]
}
```

## Data anonymization algorithms

### Overview


|anonymization method|Description|Setting Configuration|
|-----|-----|-----|
|keep|Retain the value as is.|No|
|redact|Clean the value.|Yes|
|remove|Remove the element. |No|
|perturb|Perturb the value with random noise addition.|Yes|
|dateShift|Shift the value using the Date-shift method.|Yes|
|cryptoHash|Transform the value using Crypto-hash method.|Yes|
|encrypt|Transform the value using Encrypt method.|Yes|
|substitute|Substitute the value to a predefined value.|Yes|
|refreshUID|replace with a non-zero length UID|No|

The True/False values in the `Setting Configuration` column above indicates whether the algorithm needs _defaultSettings_ and _customSettings_.

### Redact 

The value will be erased by default. But for age (AS), date (DA) and date time (DT), users can enable partial redact in setting as follow:

|Parameters|Description|Valid Value|Affected VR|Required|default value|
|----|------|--|--|--|--|
|enablePartialAgesForRedact|If the value is set to true, only age values over 89 will be redacted.|boolean| AS |False|False|
|enablePartialDatesForRedact|If the value is set to true, date, dateTime will keep year. e.g. 20210130 -> 20210101|boolean|DA, DT|False|False|

Here is a sample rule using redact method. It uses _defaultSettings_ which enables partial redact both for age, date and dateTime:
```
{
    "rules": [
        {"tag": "(0010,0020)","method": "redact"},
    ],
    "defaultSettings":[
        {"redact":{"enablePartialAgesForRedact": true","enablePartialDatesForRedact": true}}
    ],
    "customSettings":[
    ]
}

```

### Perturb

With perturb rule, you can replace specific values by adding noise. Perturb function can be used for numeric values (ushort, short, uint, int, ulong, long, decimal, double, float). Setting for perturb includes following parameters:

|Parameters|Description|Valid Value|Required|default value|
|----|----|----|----|---|
|Span| A non-negative value representing the random noise range. For fixed range type, the noise will be sampled from a uniform distribution over [-span/2, span/2]. For proportional range type, the noise will be sampled from a uniform distribution over [-span/2 * value, span/2 * value]|Positive Integer|False|1|
|RangeType|Defines whether the span value is fixed or proportional. If type is fixed, the range will be [-span/2, span/2], and for proportional range, it will be [-span/2 * value, span/2 * value]. |Fixed, Proportional|False|proportional|
|RoundTo| specifies the number of decimal places to round to.|A value from 0 to 28|False|2|

Here is a sample rule using perturb method and using _perturbCustomerSetting_ as setting with a fixed range [-5, 5] with decimal place round to 0:
```
{
    "rules": [
        {"tag": "(0020,1010)", "method": "perturb", "settings":"perturbCustomerSetting"}
    ],
    "defaultSettings":[
        {"perturb":{ "span": "1", "roundTo": 2, "rangeType": "Proportional"}},
    ],
    "customSettings":[
        {"perturbCustomerSetting":{ "span": "10", "roundTo": 0, "rangeType": "Fixed"}},
    ]
}
```

### DateShift

With this method, the input date or dateTime value will be shifted within a specific range. Dateshift function can only be used for date (DA) and date time (DT) types. In configuration, customers can define dateShiftRange, dateShiftKey and dateShiftScope. 

|Parameters|Description|Valid Value|Required|default value|
|----|----|--|--|--|
|dateShiftRange| A non-negative value representing the dateshift range. Date value will be shifted within [-dateShiftRange, dateShiftRange] days.|positive integer|False|50|
|dateShiftKey|Key used to generate shift days.|string|False|A randomly generated string will be used as default key|
|dateShiftScope|Scopes that share the same date shift key prefix and will be shift with the same days. |SeriesInstance, StudyInstance, SOPInstance. |False|SeriesInstance|

Here is a sample rule using dateShift method on DICOM tags with VR in DA. The dateShift setting is given in _defaultSettings_ field:
```
{
    "rules": [
        {"tag": "DA",  "method": "dateshift"}
    ],
    "defaultSettings":[
        {"dateShift":{"dateShiftKey": "123", "dateShiftScope": "SeriesInstance", "dateShiftRange": "50"}}
    ],
    "customSettings":[
    ]
}
```

### CryptoHash
This function uses HMAC and emits a deterministic representation that conforms to the target DICOM VR alphabet and maximum length. `UI` values use the `2.25` UUID-derived decimal form, numeric string VRs use digits, and length-limited text VRs are capped automatically. CryptoHash rules support `AE`, `CS`, `UI`, `DS`, `IS`, `SH`, `PN`, `UC`, `LO`, `UT`, `ST`, `LT`, `UR`, `OB`, and `UN`. Other VRs require a format-aware anonymization method.

Hash representations can change between implementation versions. In particular,
`SH` output uses a hexadecimal alphabet even with `matchInputStringLength`
enabled, rather than the historical numeric-only length-matched representation.
Use a consistent key, settings, and implementation version when stable values
are required within a batch; cross-version output equality is not guaranteed.

CryptoHash support is checked against the actual selected item, not the
selector's dictionary possibilities during construction. Masked and unknown
exact selectors can therefore process supported runtime representations.
`OW` fragment sequences remain supported when encountered as fragments; ordinary
nonfragment `OW` values are unsupported and fail processing rather than being
silently changed. The selected nested-rule restrictions above still apply.
In cryptoHash setting, you can set cryptoHash key in setting.

|Parameters|Description|Valid Values|Required|default value|
|----|------|--|--|--|
|cryptoHashKey| Key for cryptoHash|string|False|A randomly generated string|
|cryptoHashType| Hash method|Defined by HashAlgorithmType|False|Sha256|
|matchInputStringLength| If true, updated value will match length of input (for string values) |true, false|False|false|

Here is a sample rule using cryptoHash on DICOM tag named PatientID with default cryptoHash setting:

```
{
    "rules": [
        {"tag": "PatientID",  "method": "cryptohash"}
    ],
    "defaultSettings":[
        {"cryptoHash":{"cryptoHashKey": "123" }}
    ],
    "customSettings":[
    ]
}
```

### Encryption
We use AES-CBC algorithm to transform the value with an encryption key, and then replace the original value with a Base64 encoded representation of the encrypted value. The algorithm generates a random and unique initialization vector (IV) for each encryption, therefore the encrypted results are different for the same input values.

Users can set encrypt key in encrypt setting.
|Parameters|Description|Valid Values|Required|default value|
|----|------|--|--|--|
|encryptKey| Key for encryption|128, 192 or 256 bit string|False|A randomly generated 256-bit string|

> **[NOTE]**
> Similar with cryptoHash function, you should use the method on those fields that accept a Base64 encoded value and avoid encrypting data fields with length limits because the Base64 encoded value will be longer than the original value.

Here is a sample rule using encrypt method on PN tags with custom setting:
```
{
    "rules": [
        {"tag": "PN", "method": "encrypt", "setting":"customEncryptSetting"}
    ],
    "defaultSettings":[
        "encrypt": {"encryptKey": "123456781234567812345678"},
    ],
    "customSettings":[
        "customEncryptSetting": {"encryptKey": "0000000000000000"},
    ]
}
```

### Substitute
Using substitue, you can specify a fixed and valid value to replace a target field. You can specify the parameter "replaceWith" in setting, which is the new value for substitute.

|Parameters|Description|Valid Values|Required|default value|
|----|------|--|--|--|
|replaceWith| new value to substitute with |string|True|"ANONYMOUS"|

Here is a sample rule using substitute method on dateTime tags and replace the value to "20000101":
```
{
    "rules": [
        {"tag": "DT", "method": "substitute", "setting":"customDateTimeSubstituteSetting"}
    ],
    "defaultSettings":[
        "substitute": {"replaceWith": "ANONYMOUS"}
    ],
    "customSettings":[
        {"customDateTimeSubstituteSetting":{"replaceWith": "20000101"}},
    ]
}
```

## Output validation
Anonymizer tool can transform the input values into an invalid output. If you enable validateOutput, it will validate against **value multiplicity**, **value types** and **format** in DICOM specification. 

For example, if using encryption method on PatientID, which is a 64 chars maximum string, the encrypted output may exceed 64 chars. If disable validateOutput, the output DICOM file may be invalid for the continuing process. If you enable validateOutput, the anonymization process will fail.

Output validation only checks value for each DICOM tag, but does not check the constraints for DICOM file. For example, if some tags are changed or removed (e.g. SOPInstanceUID is required in DICOM file and the value for SpecificCharaterSet will effect other tags's value.), the output DICOM file may be damaged. 

## Current limitations
* We only support DICOM **metadata** anonymization. The anonymization is currently unavailable for image pixel data. 
* For DICOM tag which is a Sequence of Items (SQ), we only support redact and remove methods on the entire sequence.
* The constraints among tags are not considered in output validation for now. Customers should take care of the effect when changing the tag values.

## Runtime Key Parameters

The DICOM Anonymizer supports specifying key values as optional parameters when de-identifying a specific dataset, allowing you to override the configuration-based keys at runtime.

### Usage Example

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

// Option 1: Use configuration-based keys (existing behavior)
engine.AnonymizeDataset(dataset);

// Option 2: Use runtime keys to override configuration
var runtimeKeys = new RuntimeKeySettings
{
    CryptoHashKey = "my-runtime-crypto-key",
    DateShiftKey = "my-runtime-date-key",
    EncryptKey = "my-runtime-encrypt-key-1234567890123456", // Must be 16, 24, or 32 bytes
};

engine.AnonymizeDataset(dataset, runtimeKeys);
```

### Supported Runtime Keys

- **CryptoHashKey**: Overrides the cryptographic hash key for `cryptoHash` anonymization method
- **DateShiftKey**: Overrides the date shift key for `dateShift` anonymization method  
- **EncryptKey**: Overrides the encryption key for `encrypt` anonymization method

### Benefits

1. **Per-dataset anonymization**: Different datasets can use different keys while sharing the same configuration
2. **Dynamic key generation**: Keys can be generated programmatically based on external factors
3. **Enhanced security**: Keys can be managed separately from configuration files
4. **Backward compatibility**: Existing code continues to work unchanged

### Configuration Example

Your configuration file can still contain default keys:

```json
{
  "rules": [
    {"tag": "(0010,0010)", "method": "cryptoHash"},
    {"tag": "(0010,0030)", "method": "dateShift"}
  ],
  "defaultSettings": {
    "cryptoHash": {
      "cryptoHashKey": "default-crypto-key"
    },
    "dateShift": {
      "dateShiftKey": "default-date-key",
      "dateShiftRange": 50
    }
  }
}
```

When runtime keys are provided, they take precedence over the configuration defaults.
