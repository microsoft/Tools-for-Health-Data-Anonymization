# Command Line

Use the .Net Core 3.1 SDK to build the project, and you will find a executable file and you can run the exe file as follow

```

./Dicom-Anonymization.exe -i inputFile -o outputFile [-c configFile]

```

For now, POC only supports one DICOM file as input. 

# De-Id Methods
|De-id Method|Description|Need Setting|
|-----|-----|-----|
|keep|Retains the value as is.|False|
|redact|Clean the value.|true|
|remove|Remove the element. |false|
|perturb|Perturb the value with random noise addition.|true|
|dateShift|Shifts the value using the Date-shift algorithm.|true|
|cryptoHash|Transforms the value using Crypto-hash method.|true|
|encrypt|Transforms the value using Encrypt method.|true|
|substitute|Substitutes the value to a predefined value.|true|
|refreshUID|replace with a non-zero length UID|false|
# De-Id Configuration

If `-c configFile` is not given, the tool will default use "configuration.json" in the same directory with exe tool. Uses can edit configuration file to define de-id methods for different DICOM tags.

The configuration file has four sections, and specified in JSON format.

|Fields|Description|
|----|----|
|rules|De-ID rules for tags or VR.|
|profile （Not applicable for now) |Profile that determines which tags to keep or remove.|
|defaultSettings|Default settings for de-id functions. Default settings will be used if not specify settings in rules.|
|customizedSettings|Customized settings for de-id functions.|


Here is a sample configuration format :
```
{
    "rules": [
        {"tag": "(0010,1010)","method": "perturb",}, //AgeString, should be positive.
        {"tag": "(0008,0010)","method": "perturb", "params":{ "span":"5"}}, 
        {"tag": "(0020,1010)", "method": "perturb", "params":{ "span":"5"}, "setting":"perturbCustomerSetting"}, 
        {"tag": "(0040,xxxx)",  "method": "redact" }, 
        {"tag": "PatientID",  "method": "cryptohash"},
        {"VR": "PN", "method": "encrypt"}, //Patient Name
        {"VR": "DA",  "method": "dateshift"}, //Date
        {"VR": "DT", "method": "redact"} //Date Time
    ],

    "profiles":[
        "MINIMAL_KEEP_LIST_PROFILE",
    ],
    "defaultSettings":[
        {"perturbDefaultSetting":{ "span": "10", "roundTo": 2, "rangeType": "Fixed"}},
        {"dateShiftDefautSetting":{"dateShiftKey": "123", "dateShiftScope": "SeriesInstance", "dateShiftRange": "50"}}// Scope could be SeriesInstance, StudyInstance and SOPInstance    
        {"cryptoHashDefaultSetting":{"cryptoHashKey": "123", "cryptoHashFunction": "sha256" }}, // function could be sha128, sha256, sha512
        {"RedactDefaultSetting":{"enablePartialAgesForRedact": true, "enablePartialDatesForRedact": true}},
    ],
    "cutomizedSettings":[
        {"perturbCustomerSetting":{ "span": "1", "roundTo": 2, "rangeType": "Proportional"}},
    ]
}

```

## How to set rules

Users can list de-id rules for individual DICOM tag by tag value or tag name as well as a set of tags using masked value or DICOM VR. Ex：
```
{"tag": "(0010,1010)","method": "perturb"}, 
{"tag": "(0040,xxxx)",  "method": "redact"},
{"tag": "PatientID",  "method": "cryptohash"},
{"VR": "PN", "method": "encrypt"},
```
Here are parameters in each rules:

|Fields|Description| Valid Value|Required|default value|
|--|-----|-----|--|--|
|tag|Used to define DICOM elements |1. Tag Value, e.g. (0010, 0010) or 0010,0010 or 00100010. <br>2. Tag Name. e.g. PatientName. <br> 3. Masked DICOM Tag. e.g. (0010, xxxx) or (xx10, xx10). <br> 4. DICOM VR. e.g. PN, DA.|True|| 
|method|De-ID method.| keep, redact, perturb, dateshift, encrypt, cryptohash, substitute, refreshID, remove.| True||
|setting| Setting for de-id methods. Users can add customized settings in the field of "customizedSettings" and specify setting's name here. |valid setting's name |False|Default setting in the field of "defaultSettings"|
|params|parameters override setting for de-id methods.|valid parameters|False|null|

Each DICOM tag could only be de-id one time, if two rules have conflict on one tag, only the former rule will be applied. 
## How to customize settings

 Users could add customized settings with unique name which could be indexed in the field of "rules". Since parameters in settings are varied for different de-id methods. users should take care of the inconsistency between de-id methods and settings.

### Perturb Setting

With perturbation rule, you can replace specific values by adding noise. Perturb function could be used for any of numeric values including (ushort, short, uint, int, ulong, long, decimal, double, float). Applicable DICOM VR: AS, DS, FL, OF, FD, OD, IS, SL, SS, US, OW, UL, OL, UV, OV.

|Parameters|Description|Valid Value|Required|default value|
|----|----|----|----|---|
|Span| A non-negative value representing the random noise range. For fixed range type, the noise will be sampled from a uniform distribution over [-span/2, span/2]. For proportional range type, the noise will be sampled from a uniform distribution over [-span/2 * value, span/2 * value]|Positive Integer|True|
|RangeType|Define whether the span value is fixed or proportional.|fixed , proportional|False|fixed|
|RoundTo| specifies the number of decimal places to round to.|A value from 0 to 28|False|2|



### DateShift Setting

Dateshift function can only be used for date (DA) and date time (DT) types. In configuration, customers can define dateShiftRange, DateShiftKey and dateShiftScope. 

|Parameters|Description|Valid Value|Required|default value|
|----|----|--|--|--|
|dateShiftRange| A non-negative value representing the dateshift range.|positive integer|False|50|
|dateShiftKey|Key used to generate shift days.|string|False|A randomly generated string will be used as default key|
|dateShiftScope|Scopes that share the same date shift key prefix. |SeriesInstance, StudyInstance, SOPInstance. |False|SeriesInstance|

### Redact Setting

The value will be cleaned by default when using redact method. As for age (AS), date (DA) and date time (DT), users can set partial redact as follow:

|Parameters|Description|Valid Value|Affected VR|Required|default value|
|----|------|--|--|--|--|
|enablePartialAgesForRedact|If the value is set to true, only age values over 89 will be redacted.|boolean| AS |False|False|
|enablePartialDatesForRedact|If the value is set to true, date, dateTime will keep year. e.g. 20210130 -> 20210101|boolean|DA, DT|False|False|

### CryptoHash Setting

Users can set cryptoHash key and cryptoHash function (only support sha256 for now) for cryptoHash.
|Parameters|Description|Valid Values|Required|default value|
|----|------|--|--|--|
|cryptoHashKey| Key for cryptoHash|string|False|A randomly generated string|
|cryptoHashFunction| CryptoHash function |sha256|False|sha256|

### Encryption Setting

Users can set encrypt key and encrypt function (only support AES for now) in encrypt setting.
|Parameters|Description|Valid Values|Required|default value|
|----|------|--|--|--|
|encryptKey| Key for encryption|128, 192 or 256 bit string|False|A randomly generated 256-bit string|
|encryptFunction| Encrypt function |AES|False|AES|

### Substitute Setting

Substitue method just has one parameter "replaceWith" for setting, which is the new value for substitute.

|Parameters|Description|Valid Values|Required|default value|
|----|------|--|--|--|
|replaceWith| New value for substitute|string|True||


# Nested DICOM Data

For now, we only support define de-id root level tags.
If DICOM tag is SQ (sequence of items), only "redact" method could be used to remove entire sequence.


