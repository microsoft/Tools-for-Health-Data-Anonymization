# FHIR Anonymizer

[![Build Status](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_apis/build/status/CI%20Build?branchName=master)](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_build/latest?definitionId=19&branchName=master)

## TOC
[Overview](#overview)  
[Quickstarts](#quickstarts)  
[Samples](#samples)  
&nbsp;&nbsp; [Sample configuration file for HIPAA Safe Harbor method](#sample-configuration-file-for-hipaa-safe-harbor-method)  
[Concepts](#concepts)  
[How-to guides](#how-to-guides)  
&nbsp;&nbsp; [How to deploy the FHIR Anonymizer ADF pipeline](#deploy-the-fhir-anonymizer-adf-pipeline)  
[Reference](#reference)  
&nbsp;&nbsp; [FAQ](#faq)  
&nbsp;&nbsp; [Glossary](#glossary)  
[Resources](#resources)  
[Contributing](#contributing)

# Overview

FHIR Anonymizer is an open-source project that helps anonymize healthcare [FHIR](https://www.hl7.org/fhir/) data, on-premises or in the cloud, for secondary usage such as research, public health, and more. The FHIR Anonymizer released to open source on Thursday, March 5th, 2020.

The FHIR Anonymizer uses a [configuration file](#configuration-file-format) specifying the de-identification settings to anonymize the data. The anonymizer includes a [command-line tool](#use-the-command-line-tool) that can be used on-premises or in the cloud. It also comes with a [script](#tbd-adf-script) to create an ADF pipeline that reads data from Azure blob store and writes anonymized data back to a specified blob store.

This repo contains a [sample configuration file](#tbd-link-to-config-file) to de-identify 17 data elements as per [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method for de-identification. Customers can update the configuration file or create their own configuration file as per their needs by following the [documentation](#configuration-file-format).  

This open source project is fully backed by the Microsoft Healthcare team, but we know that this project will only get better with your feedback and contributions. We are leading the development of this code base, and test builds and deployments daily.

## Features

* Configuration of the data elements that need to be de-identified 
* Configuration of the de-identification method for each data element (keeping, redacting, or Date-shifting) 
* Running the tool as part of Azure Data Factory to support de-identification of the data flows.  
* Running the tool on premise to de-identify a dataset locally 

# Quickstarts

## Use the command line tool

FHIR Anonymizer can be used as a command-line tool to anonymize a folder containing FHIR resource files. The Anonymizer expects a configuration file by option "-c". If not specified, a sample configuration file named **configuration-sample.json** will be used.

| Option | Name | Description |
| ----- | ----- | ----- |
| -i | inputFolder | Required. Folder to locate input resource files. |
| -o | outputFolder | Required. Folder to save anonymized resource files. |
| -c | configFile | Optional, default "configuration-sample.json" in current directory. Anonymizer configuration file path. |
| -b | bulkData | Optional, default false. Resource file is in bulk data format (.ndjson). |
| -r | recursive | Optional, default false. Process resource files in input folder recursively. |
| -v | verbose | Optional, default false. Provide additional details in processing. |


Example usage to anonymize FHIR resource files in a folder: 
```
> .\Fhir.Anonymizer.Tool.exe -i myInputFolder -o myOutputFolder
```
# Samples

## Sample configuration file for HIPAA Safe Harbor method

[HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) guideline mentions that the following identifiers of the individual or of relatives, employers, or household members of the individual, be removed. 

The following table describes the treatment of the guideline in our sample configuration file. We strongly recommend that you review the guideline and verify the implementation before using this configuration file for your requirements. 

|Identifier| Redacted Fields | Remarks |
| ----- | ----- | ----- |
| (A) Names |||
| (B) All geographic subdivisions smaller than a state, including street address, city, county, precinct, ZIP code, and their equivalent geocodes, except for the initial three digits of the ZIP code if, according to the current publicly available data from the Bureau of the Census: <br/> (1) The geographic unit formed by combining all ZIP codes with the same three initial digits contains more than 20,000 people; and <br/>(2) The initial three digits of a ZIP code for all such geographic units containing 20,000 or fewer people is changed to 000 |||
| (C) All elements of dates (except year) for dates that are directly related to an individual, including birth date, admission date, discharge date, death date, and all ages over 89 and all elements of dates (including year) indicative of such age, except that such ages and elements may be aggregated into a single category of age 90 or older |||
| (D) Telephone numbers |||
| (E) Fax numbers |||
| (F) Email addresses |||
| (G) Social security numbers |||
| (H) Medical record numbers |||
| (I) Health plan beneficiary numbers |||
| (J) Account numbers |||
| (K) Certificate/license numbers |||
| (L) Vehicle identifiers and serial numbers, including license plate numbers |||
| (M) Device identifiers and serial numbers |||
| (N) Web Universal Resource Locators (URLs) |||
| (O) Internet Protocol (IP) addresses |||
| (P) Biometric identifiers, including finger and voice prints |||
| (Q) Full-face photographs and any comparable images |||
| (R) Any other unique identifying number, characteristic, or code, except as permitted by paragraph (c) of this section; and |||

# Concepts

## How it works
The FHIR Anonymizer uses a configuration file specifying different parameters as well as de-identification methods for different data-elements and datatypes. 

FHIR Anonymizer comes with a default configuration file, which is based on the [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method. You can modify the configuration file as needed based on the information provided below.

## Configuration file format

The configuration is specified in JSON format. It has three high-level sections. Two of these sections, namely _pathRules_, and _typeRules_ are meant to specify de-identification methods for data elements. De-identification configuration specified in the _pathRules_ section override the corresponding configurations in the _typeRules_ section. The third section named _parameters_ affect global behavior.

Here is a sample configuration:

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

### Path Rules
Path rules are key-value pairs that can be used to specify the de-identification methods for individual elements. Ex:

```json
"Patient.address.state": "keep"
```
The elements can be specified using [FHIRPath](http://hl7.org/fhirpath/) syntax. The method can be one from the following table.

|Method| Applicable to | Description
| ----- | ----- | ----- |
|keep|All elements| Retains the value as is. |
|redact|All elements| Removes the element. See the parameters section below to handle special cases.|
|dateShift|Elements of type date, dateTime, and instant | Shifts the value using the Date Shift algorithm described below.

### Type Rules
Type rules are key-value pairs that can be used to specify the de-identification methods at the datatype level. Ex:

```json
"date": "dateShift"
```
The datatypes can be any of the [FHIR datatypes](https://www.hl7.org/fhir/datatypes.html). The method can be one from the following table.

|Method| Applicable to | Description
| ----- | ----- | ----- |
|keep|All datatypes | Retains the value as is. |
|redact|All datatypes| Removes the element. See the parameters section below to handle special cases. |
|dateShift|date, dateTime, and instant datatypes | Shifts the value using the Date Shift algorithm described below.

### Parameters
Parameters affect the de-identification methods specified in the type rules and path rules. 

|Method| Parameter | Affected fields | Valid values | Default value | Description
| ----- | ----- | ----- | ----- | ----- | ----- |
| dateShift |dateShiftKey|date, dateTime, instant fields| string|A randomly generated string|This key in conjunction with the FHIR resource id is used in the Date Shift algorithm as described below in this document. 
| redact | enablePartialAgesForRedact |Age fields | boolean | false | If the value is set to **true**, only age over 89 will be redacted. |
| redact | enablePartialDatesForRedact  | date, dateTime, instant fields | boolean | false | If the value is set to **true**, date, dateTime, instant will keep year if indicative age is not over 89. |
| redact | enablePartialZipCodesForRedact  | Zip Code fields | boolean | false | If the value is set to **true**, Zip Code will be redacted as per the HIPAA Safe Harbor rule. |
| redact | restrictedZipCodeTabulationAreas  | Zip Code fields | a JSON array | empty array | This configuration is used only if enablePartialZipCodesForRedact is set to **true**. This field contains the list of zip codes for which the first 3 digits will be converted to 0. As per the HIPAA Safe Harbor, this list will have the Zip Codes  having population less than 20,000 people. |


## Date-shift algorithm
You can specify dateShift as a de-identification method in the configuration file. With this method, the input date/dateTime/instant value will be shifted within a 100-day differential. The following algorithm is used to shift the target dates:

### Input
* A date/dateTime/instant value (required)
* FHIR resource id (optional). If not specified, an empty string will be used.
* Date shift key (optional). If not specified, a randomly generated string will be used.

### Output
* A shifted date/datetime/instant value

### Steps
1. Create a string by combining FHIR resource id and date shift key.
2. Feed the above string to hash function to get an integer between [-50, 50]. 
3. Use the above integer as the offset to shift the input date/dateTime/instant value

### Note

1. If the input date/dateTime/instant value does not contain exact day, like "yyyy", "yyyy-MM", there's no date can be shifted and redaction will be applied.
2. If the input date/dateTime/instant value is indicative of age over 89, it will be redacted (including year) according to HIPAA Safe Harbor Method.
3. If the input dateTime/instant value contains time, time will be redacted. Time zone will keep unchanged.

# Reference
## Current limitations
1. We only support FHIR data in R4, JSON format. Support for XML and STU 3 is planned.
2. Date Shifting algorithm shifts the dates within a resource by the same random amount. We are working on the ability to shift the dates by the same random amount across resources. 

## Known issues

## FAQ
### How can we use FHIR Anonymizer to anonymize HL7 v2.x data
You can build a pipeline to use [FHIR converter](https://github.com/microsoft/FHIR-Converter) to convert HL7 v2.x data to FHIR format, and subsequently use FHIR Anonymizer to anonymize your data. 
### What other de-identification methods will be supported?

### Can we use custom de-identification methods?
Currently you can use the prebuilt de-identification methods and control their behavior by passing parameters. We are planning to support custom de-identification methods in future.


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
