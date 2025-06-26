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
Parameters in each rules:

|Fields|Description| Valid Value|Required|default value|
|--|-----|-----|--|--|
|tag|Used to define DICOM elements |1. Tag Value, e.g. (0010, 0010) or 0010,0010 or 00100010. <br>2. Tag Name. e.g. PatientName. <br> 3. Masked DICOM Tag. e.g. (0010, xxxx) or (xx10, xx10). <br> 4. DICOM VR. e.g. PN, DA.|True|null| 
|method|anonymization method| keep, redact, perturb, dateshift, encrypt, cryptohash, substitute, refreshUID, remove.| True|null|
|setting| Setting for anonymization method. Users can add custom settings in the field of "customSettings" and specify setting's name here. |valid setting's name |False|Default setting in the field of "defaultSettings"|
|params|parameters override setting for anonymization methods.|valid parameters|False|null|

Each DICOM tag can only be anonymized once, if two rules have conflicts on one tag, only the former rule will be applied.

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
This function use HMAC-SHA256 algorithm and outputs a Hex encoded representation (for example, a3c024f01cccb3b63457d848b0d2f89c1f744a3d). The length of output string is 64 bytes. You should pay attention to the length limitation of output DICOM file.
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
