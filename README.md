# FHIR Anonymizer
FHIR Anonymizer is an open-source project that helps anonymize healthcare FHIR data, on-premises or in the cloud, for secondary usage such as research, public health, and more. The FHIR Anonymizer released to open source on Thursday March 5th, 2020.

FHIR Anonymizer includes a command-line tool that can be used on-premises on a set of data using a configuration file that specifies the de-identification settings. The FHIR Anonymizer can also be integrated in Azure Data Factory flow to anonymize data in the cloud. 

This repo contains a configuration file to de-identify 18 data elements as per [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method for de-identification. Customers can update the configuration file or create their own configuration file as per their needs.  

This open source project is fully backed by the Microsoft Healthcare team, but we know that this project will only get better with your feedback and contributions. We are leading the development of this code base, and test builds and deployments daily.

## Features

* Configuration of the data elements that need to be de-identified 
* Configuration of the de-identification method for each data element (masking, redacting and Date Shifting) 
* Running the tool on premise to de-identify a dataset locally 
* Running the tool as part of Azure Data Factory to support de-identification of the data flows.  

## How it works
The FHIR Anonymizer uses a configuration file to specify de-identification methods for different data-elements and datatypes, and other parameters to anonymize the data. 

FHIR Anonymizer comes with a default configuration file, which is based on the HIPAA safe harbor method. You can modify the configuration file as needed based on the information provided below.

### The configuration file

The configuration is specified in JSON format. It has three high-level sections. Two of these sections namely pathRules, and typeRules are meant to specify de-identification methods for data elements. De-identification configuration specified in the pathRules section override the corresponding configurations in the typeRules section. The third section named parameters affect global behavior.

```json
{
  "pathRules": {
    "Patient.address.state": "keep",
    "Patient.address.country": "redact"
  },
  "typeRules": {
    "date": "dateShift",
    "Address": "redact"
  },
  "parameters": {
    "dateShiftKey": "",
    "enablePartialAgesForRedact": true
}
```

#### Path Rules
Path rules are key-value pairs that can be used to specify the de-identification methods for individual elements. Ex:

```json
"Patient.address.state": "keep"
```
The elements can be specified using [FHIRPath](http://hl7.org/fhirpath/) syntax. The method can be one from the following table.

|Method| Applicable to | Description
| ----- | ----- | ----- |
|keep|All elements| Retains the value as is. |
|redact|All elements| Removes the element completely. |
|dateShift|Elements of type date, dateTime, and instance | Shifts the value using the Date Shift algorithm described below.

#### Type Rules
Type rules are key-value pairs that can be used to specify the de-identification methods at the datatype level. Ex:

```json
"date": "dateShift"
```
The datatypes can be any of the [FHIR datatypes](https://www.hl7.org/fhir/datatypes.html). The method can be one from the following table.

|Method| Applicable to | Description
| ----- | ----- | ----- |
|keep|All datatypes | Retains the value as is. |
|redact|All datatypes| Removes the element completely. |
|dateShift|date, dateTime, and instance datatypes | Shifts the value using the Date Shift algorithm described below.

#### Parameters
Parameters affect the global de-identification behavior as described below:

|Parameter| Valid Values | Description
| ----- | ----- | ----- |
| dateShiftKey | true, false |  |
| enablePartialAgesForRedact | true, false |  |
| enablePartialDatesForRedact | true, false |  |
| enablePartialZipCodesForRedact | true, false |  |
| restrictedZipCodeTabulationAreas | \<any string\> |  |

### Date-shift algorithm
You can specify dateShift as a de-identification method in the configuration file. The following algorithm is used to shift the target dates:

#### Input
* A date or dateTime value (required)
* FHIR resource id (required)
* An encrypted base64-encoded key (optional)

#### Output
* A shifted date or datetime value

#### Steps
1. If the key is empty, generate a random string as key.
2. Create a seed by combining the key and FHIR resource id.
3. Use the above seed in BKDR hash function to get an integer between [-50, 50]. 
4. Use the above integer as the offset to shift the date or dateTime value

#### Note
Why is FHIR Resource ID used in the date-shift algorithm?

If we generate the offset amount only by the key, every date value in the dataset will be shifted with the same offset. However, if resource ID is involved, the offset will be different among different resources, bringing in more randomness. Besides, using Resource Id ensures that all the dates within the same resource have the same offset, which helps avoid the conflict between dates. For example, if the offset is different within the same resource, the start value may be later than the end value of a Period instance.

## How to use it

### Using it as a command-line tool

FHIR Anonymizer can be used as a command-line tool to anonymize individual files containing FHIR bundle or a folder containing multiple such files. The Anonymzer expects the configuration file by the name **Configuration.json** in the current directory. 

Example usage to anonymize single file: 
```
> .\Fhir.DeIdentification.Tool.exe -i myinput.json -o myoutput.json
```

## FAQ
### What FHIR versions are supported?
Currently, the FHIR Anonymizer support FHIR data in STU3 or R4 format.  
### How can we use FHIR Anonymizer to anonymize HL7 v2.x data
You can build a pipeline to use [FHIR converter](https://github.com/microsoft/FHIR-Converter) to convert HL7 v2.x data to FHIR format, and subsequently use FHIR Anonymizer to anonymize your data. 

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

FHIR® is the registered trademark of HL7 and is used with the permission of HL7.
