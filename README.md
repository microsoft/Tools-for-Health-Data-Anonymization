# FHIR Tools for Anonymization

[![Build Status](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_apis/build/status/CI%20Build?branchName=master)](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_build/latest?definitionId=23&branchName=master)

## TOC
[Overview](#overview)  
[Quickstarts](#quickstarts)  
[Tutorials](#tutorials)  
&nbsp;&nbsp; [Anonymize FHIR data using Azure Data Factory](#anonymize-fhir-data-using-azure-data-factory)  
[Samples](#samples)  
&nbsp;&nbsp; [Sample configuration file for HIPAA Safe Harbor method](#sample-configuration-file-for-hipaa-safe-harbor-method)  
[Concepts](#concepts)  
&nbsp;&nbsp; [How the engine works](#how-the-engine-works)  
[Reference](#reference)  
&nbsp;&nbsp; [The command line tool](#the-command-line-tool)  
&nbsp;&nbsp; [Configuration file format](#configuration-file-format)  
&nbsp;&nbsp; [Sample rules](#sample-rules-using-fhirpath)  
&nbsp;&nbsp; [Date-shift algorithm](#date-shift-algorithm)  
&nbsp;&nbsp; [Crypto-hash method](#Crypto-hash-method)  
&nbsp;&nbsp; [Encrypt method](#Encrypt-method)  
&nbsp;&nbsp; [Substitute method](#Substitute-method)  
&nbsp;&nbsp; [Perturb method](#Perturb-method)  
[Resources](#resources)  
&nbsp;&nbsp; [FAQ](#faq)  
[Contributing](#contributing)

# Overview

FHIR Tools for Anonymization is an open-source project that helps anonymize healthcare [FHIR](https://www.hl7.org/fhir/) data, on-premises or in the cloud, for secondary usage such as research, public health, and more. The project released to open source on Friday, March 6th, 2020.

The anonymization capability is available to the users in the following forms:
1. A [command-line tool](#the-command-line-tool) that can be used on-premises or in the cloud to anonymize data. 
2. An ADF pipeline. It comes with a [script](#anonymize-fhir-data-using-azure-data-factory) to create a pipeline that reads data from Azure blob store and writes anonymized data back to a specified blob store.
3. [De-identified $export](#how-to-perform-de-identified-export-operation-on-the-fhir-server) operation in the [FHIR server for Azure](https://github.com/microsoft/fhir-server).

The core engine uses a [configuration file](#configuration-file-format) specifying the de-identification settings. This repo contains a sample [safe harbor configuration file](#sample-configuration-file-for-hipaa-safe-harbor-method) to help de-identify data elements as per [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method for de-identification. Customers can update the configuration file or create their own configuration file as per their needs by following the [documentation](#configuration-file-format).  

This open source project is fully backed by the Microsoft Healthcare team, but we know that this project will only get better with your feedback and contributions. We are leading the development of this code base, and test builds and deployments daily.

FHIR® is the registered trademark of HL7 and is used with the permission of HL7. Use of the FHIR trademark does not constitute endorsement of this product by HL7.

## Features

* Support anonymization of FHIR R4 and STU 3 data in json as well as ndjson format
* Configuration of the data elements that need to be de-identified 
* Configuration of the [de-identification methods](#fhir-path-rules) for each data element
* Ability to create a de-identification pipeline in Azure Data Factory
* Ability to run the tool on premise to de-identify a dataset locally

# Quickstarts

## Building the solution
Use the .Net Core 3.1 SDK to build FHIR Tools for Anonymization. If you don't have .Net Core 3.1 installed, instructions and download links are available [here](https://dotnet.microsoft.com/download/dotnet-core/3.1).

## Get sample FHIR files
This repo contains a few [sample](samples/) FHIR files that you can download. These files were generated using  [Synthea&trade; Patient Generator](https://github.com/synthetichealth/synthea). 

You can also export FHIR resource from your FHIR server using [Bulk Export](https://docs.microsoft.com/en-us/azure/healthcare-apis/configure-export-data).

## Anonymize FHIR data using the command line tool
Once you have built the command line tool, you will find two executable files for R4 and STU 3 respectively: 

1. Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe in the $SOURCE\src\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool\bin\Debug|Release\netcoreapp3.1 folder. 

2. Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool.exe in the $SOURCE\src\Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool\bin\Debug|Release\netcoreapp3.1 folder.

 You can use these executables to anonymize FHIR resource files in a folder.   
```
> .\Microsoft.Health.Fhir.Anonymizer.<version>.CommandLineTool.exe -i myInputFolder -o myOutputFolder
```
See the [reference](#the-command-line-tool) section for usage details of the command line tool.

# Tutorials

## Anonymize FHIR data using Azure Data Factory

In this tutorial, you use the Azure PowerShell to create a Data Factory and a pipeline to anonymize FHIR data. The pipeline reads from an Azure blob container, anonymizes it as per the configuration file, and writes the output to another blob container. If you're new to Azure Data Factory, see [Introduction to Azure Data Factory](https://docs.microsoft.com/en-us/azure/data-factory/introduction).

Tutorial steps:

* Use a PowerShell script to create a data factory pipeline.
* Trigger on-demand pipeline run.
* Monitor the pipeline and activity runs.

### Prerequisites

* **Azure subscription**: If you don't have an Azure subscription, create a [free account](https://azure.microsoft.com/free/) before you begin.
* **Azure storage account**: Azure Blob storage is used as the _source_ & _destination_ data store. If you don't have an Azure storage account, see the instructions in [Create a storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal). 
* **Azure PowerShell**: Azure PowerShell is used for deploying azure resources. If you don't have Azure PowerShell installed, see the instructions in [Install the Azure PowerShell module](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-3.4.0)
* **.Net Core 3.1**: Use .Net Core 3.1 sdk to build FHIR Tools for Anonymization. If you don't have .Net Core 3.1 installed, instructions and download links are available [here](https://dotnet.microsoft.com/download/dotnet-core/3.1).

#### Prepare azure storage resource container

Create a source and a destination container on your blob store. Upload your FHIR files to the source blob container. The pipeline will read the files from the source container and upload the anonymized files to the destination container.

You can also export FHIR resources from a FHIR server using [Bulk Export](https://github.com/microsoft/fhir-server/blob/master/docs/BulkExport.md) and put the data to the source blob container.

#### Log in to Azure using PowerShell
1. Launch **PowerShell** on your machine. Keep PowerShell open until the end of this tutorial. If you close and reopen, you need to run these commands again.

2. Run the following command, and enter the Azure user name and password to sign in to the Azure portal:

    ```powershell
    Connect-AzAccount
    ```

3. Run the following command to view all the subscriptions for this account:

    ```powershell
    Get-AzSubscription
    ```

4. If you see multiple subscriptions associated with your account, run the following command to select the subscription that you want to work with. Replace **SubscriptionId** with the ID of your Azure subscription:

    ```powershell
    Select-AzSubscription -SubscriptionId "<SubscriptionId>"
    ```

### Create Data Factory pipeline

1. Enter the project folder $SOURCE\src\Microsoft.Health.Fhir.Anonymizer.\<version>.AzureDataFactoryPipeline. Locate _AzureDataFactorySettings.json_ in the project and replace the values as described below.

> **[!NOTE]**
> dataFactoryName can contain only lowercase characters or numbers, and must be 3-19 characters in length.
```json
{
  "dataFactoryName": "<Custom Data Factory Name>",
  "resourceLocation": "<Region for Data Factory>",
  "sourceStorageAccountName": "<Storage Account Name for source files>",
  "sourceStorageAccountKey": "<Storage Account Key for source files>",
  "destinationStorageAccountName": "<Storage Account Name for destination files>",
  "destinationStorageAccountKey": "<Storage Account Key for destination files>",
  "sourceStorageContainerName": "<Storage Container Name for source files>",
  "sourceContainerFolderPath": "<Optional: Directory for source resource file path>",
  "destinationStorageContainerName": "<Storage Container Name for destination files>",
  "destinationContainerFolderPath": "<Optional: Directory for destination resource file path>",
  "activityContainerName": "<Container name for anonymizer tool binraries>"
}
```

2. Define the following variables in PowerShell. These are used for creating and configuring the execution batch account. 
```powershell
> $SubscriptionId = "SubscriptionId"
> $BatchAccountName = "BatchAccountName. New batch account would be created if account name is null or empty."
> $BatchAccountPoolName = "BatchAccountPoolName"
> $BatchComputeNodeSize = "Node size for batch node. Default value is 'Standard_d1'"
> $ResourceGroupName = "Resource group name for Data Factory. Default value is $dataFactoryName + 'resourcegroup'"
```

3. Run powershell scripts to create data factory pipeline

```powershell
> .\DeployAzureDataFactoryPipeline.ps1 -SubscriptionId $SubscriptionId -BatchAccountName $BatchAccountName -BatchAccountPoolName $BatchAccountPoolName -BatchComputeNodeSize $BatchComputeNodeSize -ResourceGroupName $ResourceGroupName
```

### Trigger and monitor pipeline run from PowerShell

Once a Data Factory pipeline is created, use the following command to trigger pipeline run from PowerShell:

```powershell
> .\DeployAzureDataFactoryPipeline.ps1 -RunPipelineOnly -SubscriptionId $SubscriptionId -BatchAccountName $BatchAccountName -BatchAccountPoolName $BatchAccountPoolName -BatchComputeNodeSize $BatchComputeNodeSize -ResourceGroupName $ResourceGroupName
```

Pipeline run result will be shown in console. You will also find stdout and stderr resource links in the result.

```
[2020-01-22 02:04:20] Pipeline is running...status: InProgress
[2020-01-22 02:04:43] Pipeline is running...status: InProgress
[2020-01-22 02:05:06] Pipeline run finished. The status is: Succeeded

ResourceGroupName : adfdeid2020resourcegroup
DataFactoryName   : adfdeid2020
RunId             : d84a33a0-aceb-4fd1-b37c-3c06c597201b
PipelineName      : AdfDeIdentificationPipeline
LastUpdated       : 1/22/2020 6:04:51 AM
Parameters        : {}
RunStart          : 1/22/2020 6:04:15 AM
RunEnd            : 1/22/2020 6:04:51 AM
DurationInMs      : 35562
Status            : Succeeded
Message           :
Activity 'Output' section:
"exitcode": 0
"outputs": [
  "https://deid.blob.core.windows.net/adfjobs/d04171de-aba5-4f43-a0fc-456a2f004382/output/stdout.txt",
  "https://deid.blob.core.windows.net/adfjobs/d04171de-aba5-4f43-a0fc-456a2f004382/output/stderr.txt"
]
"computeInformation": "{\"account\":\"adfdeid2021batch\",\"poolName\":\"adfpool\",\"vmSize\":\"standard_d1_v2\"}"
"effectiveIntegrationRuntime": "DefaultIntegrationRuntime (West US)"
"executionDuration": 31
"durationInQueue": {
  "integrationRuntimeQueue": 0
}
"billingReference": {
  "activityType": "ExternalActivity",
  "billableDuration": {
    "Managed": 0.016666666666666666
  }
}
```
### Trigger and monitor pipeline run from Azure Data Factory portal

You can trigger the pipeline by clicking on the *Add Trigger* button in the pipelines view of the Data Factory portal. You can also view the pipeline run details by going to Monitor => "Pipeline Runs" => Select Pipeline run => show activity outputs.

### Clean up resources

The PowerShell script implicitly creates a resource group by appending 'resourcegroup' to the Data Factory name provided by you in _AzureDataFactorySettings.json_. 

If you want to cleanup resources, delete that resource group in addition to any other resources you may have explicitly created as part of this tutorial.

# Samples

## Sample configuration file for HIPAA Safe Harbor method
FHIR Tools for Anonymization comes with a sample configuration file to help meet the requirements of HIPAA Safe Harbor Method (2)(i). HIPAA Safe Harbor Method (2)(ii) talks about "actual knowledge", which is out of scope for this project.

Out of the 18 identifier types mentioned in HIPAA Safe Harbor method (2)(i), this configuration file deals with the first 17 identifier types (A-Q). The 18th type, (R), is unspecific and hence not considered in this configuration file. 

This configuration file is provided in a best-effort manner. We **strongly** recommend that you review the HIPAA guidelines as well as the implementation of this project before using it for you anonymization requirements. 


The safe harbor configuration files can be accessed via [R4](src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/configuration-sample.json) and [STU 3](src/Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool/configuration-sample.json) links.

# Concepts

## How the engine works
The anonymization engine uses a configuration file specifying different parameters as well as de-identification methods for different data-elements and datatypes. 

The repo contains a [sample configuration file](#sample-configuration-file-for-hipaa-safe-harbor-method), which is based on the [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method. You can modify the configuration file as needed based on the information provided below.

# Reference

## The command line tool

The command-line tool can be used to anonymize a folder containing FHIR resource files. Here are the parameters that the tool accepts:

| Option | Name | Optionality | Default | Description |
| ----- | ----- | ----- |----- |----- |
| -i | inputFolder | Required | | Folder to locate input resource files. |
| -o | outputFolder | Required | |  Folder to save anonymized resource files. |
| -c | configFile | Optional |configuration-sample.json | Anonymizer configuration file path. It reads the default file from the current directory. |
| -b | bulkData | Optional| false | Resource file is in bulk data format (.ndjson). |
| -r | recursive | Optional | false | Process resource files in input folder recursively. |
| -v | verbose | Optional | false | Provide additional details during processing. |
| -s | skip | Optional | false | Skip files that are already present in the destination folder. |
| --validateInput | validateInput | Optional | false | Validate input resources against structure, cardinality and most value domains in FHIR specification. Detailed report can be found in verbose log. |
| --validateOutput | validateOutput | Optional | false | Validate anonymized resources against structure, cardinality and most value domains in FHIR specification. Detailed report can be found in verbose log. |

Example usage to anonymize FHIR resource files in a folder: 
```
> .\Microsoft.Health.Fhir.Anonymizer.<version>.CommandLineTool.exe -i myInputFolder -o myOutputFolder
```

## Configuration file format

The configuration is specified in JSON format. It has three high-level sections. One of these sections, namely _fhirVersion_ specify the configuration file's version for anonymizer. The second section named _fhirPathRules_ is meant to specify de-identification methods for data elements. The third section named _parameters_ affects global behavior. _fhirPathRules_ are executed in the order of appearance in the configuration file. 

Here is a sample configuration for R4:

```json
{
  "fhirVersion": "R4",
  "fhirPathRules": [
    {"path": "nodesByType('Extension')", "method": "redact"},
    {"path": "Organization.identifier", "method": "keep"},
    {"path": "nodesByType('Address').country", "method": "keep"},
    {"path": "Resource.id", "method": "cryptoHash"},
    {"path": "nodesByType('Reference').reference", "method": "cryptoHash"},
    {"path": "Group.name", "method": "redact"}
  ],
  "parameters": {
    "dateShiftKey": "",
    "cryptoHashKey": "",
    "encryptKey": "",
    "enablePartialAgesForRedact": true
  }
}
```
### FhirVersion Specification
| fhirVersion | Desciption |
| ----- | ----- |
|Stu3|Specify STU 3 version for the configuration file|
|R4|Specify R4 version for the configuration file|
|Empty or Null| The configuration file targets the same FHIR version as the executable.
|Other values| Other values will raise an exception.


### FHIR Path Rules
FHIR path rules can be used to specify the de-identification methods for individual elements as well as elements of specific data types. Ex:

```json
{"path": "Organization.identifier", "method": "keep"}
```
The elements can be specified using [FHIRPath](http://hl7.org/fhirpath/) syntax. The method can be one from the following table.

|Method| Applicable to | Description
| ----- | ----- | ----- |
|keep|All elements| Retains the value as is. |
|redact|All elements| Removes the element. See the parameters section below to handle special cases.|
|dateShift|Elements of type date, dateTime, and instant | Shifts the value using the [Date-shift algorithm](#date-shift-algorithm).
|perturb|Elements of numeric and quantity types| [Perturb](#Perturb-method) the value with random noise addition.  |
|cryptoHash|All elements| Transforms the value using [Crypto-hash method](#Crypto-hash-method). |
|encrypt|All elements| Transforms the value using [Encrypt method](#Encrypt-method).  |
|substitute|All elements| [Substitutes](#Substitute-method) the value to a predefined value. |
|generalize|Elements of primitive types|[Generalizes](#Generalize-method) the value into a more general, less distinguishing value.

Two extension methods can be used in FHIR path rule to simplify the FHIR path:
- nodesByType('_typename_'): return descendants of type '_typename_'. Nodes in bundle resource and contained list will be excluded. 
- nodesByName('_name_'): return descendants of node name '_name_'. Nodes in bundle resource and contained list will be excluded. 

### Parameters
Parameters affect the de-identification methods specified in the FHIR path rules. 

|Method| Parameter | Affected fields | Valid values | Default value | Description
| ----- | ----- | ----- | ----- | ----- | ----- |
| dateShift |dateShiftKey|date, dateTime, instant fields| string|A randomly generated string|This key is used to generate date-shift amount in the [Date-shift algorithm](#date-shift-algorithm). 
| dateShift |dateShiftScope|date, dateTime, instant fields| resource, file, folder | resource | This parameter is used to select date-shift scope. Dates within the same scope will be shifted the same amount. Please provide dateShiftKey together with it. |
| cryptoHash |cryptoHashKey|All hashing fields| string|A randomly generated string|This key is used for HMAC-SHA256 algorithm in [Crypto-hash method](#Crypto-hash-method). 
| encrypt |encryptKey|All encrypting fields| string|A randomly generated 256-bit string|This key is used for AES encryption algorithm in [Encrypt method](#Encrypt-method). 
| redact | enablePartialAgesForRedact |Age fields | boolean | false | If the value is set to **true**, only age values over 89 will be redacted. |
| redact | enablePartialDatesForRedact  | date, dateTime, instant fields | boolean | false | If the value is set to **true**, date, dateTime, instant will keep year if indicative age is not over 89. |
| redact | enablePartialZipCodesForRedact  | Zip Code fields | boolean | false | If the value is set to **true**, Zip Code will be redacted as per the HIPAA Safe Harbor rule. |
| redact | restrictedZipCodeTabulationAreas  | Zip Code fields | a JSON array | empty array | This configuration is used only if enablePartialZipCodesForRedact is set to **true**. This field contains the list of zip codes for which the first 3 digits will be converted to 0. As per the HIPAA Safe Harbor, this list will have the Zip Codes  having population less than 20,000 people. |

## Sample rules using FHIRPath

To retain country as well as state values of Address data type
```json
{"path": "nodesByType('Address').country | nodesByType('Address').state", "method": "keep"}
```

To date-shift date, dateTime, and instant data types
```json
{"path": "nodesByType('date') | nodesByType('dateTime') | nodesByType('instant')", "method": "dateshift"}
```

To redact the home-use Contact point
```json
{"path": "nodesByType('ContactPoint').where(use='home')","method": "redact"}
```

To perturb age fields of Condition resource by adding random noise having range ```[-3, 3]```
```json
{
  "path": "Condition.onset | Condition.abatement as Age",
  "method": "perturb",
  "span": 6,
  "rangeType": "fixed",
  "roundTo": 0
}
```
To perturb age fields of Condition resource by adding random noise having range ```[-0.1*originalAge, 0.1*originalAge]```
```json
{
  "path": "Condition.onset | Condition.abatement as Age",
  "method": "perturb",
  "span": 0.2,
  "rangeType": "proportional",
  "roundTo": 0
}
```

To generate hash of Resource Id
```json
{"path": "Resource.id", "method": "cryptoHash"}
```
To encrypt city values of Address data type
```json
{"path": "nodesByType('Address').city", "method": "encrypt"}
```

To substitute city values of Address data type with "example city"
```json
{"path": "nodesByType('Address').city", "method": "substitute", "replaceWith": "example city"}
```
To substitute Address data types with a fixed json fragement
```json
{
  "path": "nodesByType('Address')", 
  "method": "substitute", 
  "replaceWith": {
    "use":"home",
    "city": "example city",
    "state": "example state",
    "period": {
      "start": "2000-01-01"
    }
  }
}
```
To generalize age fields of Condition resource using expression to define the range mapping
```json
{
  "path": "condition.onset.value as Age",
  "method": "generalize",
  "cases":{
    "$this>=0 and $this<20": "20",
    "$this>=20 and $this<40": "40",
    "$this>=40 and $this<60": "60",
    "$this>=60 and $this<80": "80"     
  },
  "otherValues":"redact"
}
```
To generalize string data type using expression to define the value set mapping

```json
{
  "path": "patient.communication.language",
  "method": "generalize",
  "cases":{
    "$this in ('en-AU' | 'en-CA' | 'en-GB' |'en-IN' | 'en-NZ' | 'en-SG' | 'en-US')": "'en'",
    "$this in ('es-AR' | 'es-ES' | 'es-UY')": "'es'",    
  },
  "otherValues":"redact"
}
```

To generalize string data type using expression for masking

```json
{
  "path": "patient.address.postalcode",
  "method": "generalize",
  "cases":{
    "$this.startsWith('123') or $this.startsWith('234')": "$this.subString(0,2)+'****'", 
  },
  "otherValues":"redact"
  }
```
To generalize dateTime, time, date and instant type using expression

```json
{
  "path": "person.birthDate",
  "method": "generalize",
  "cases":{
    "$this >= @1990-1-1 and $this <= @2000-1-1": "@1990", 
     "$this >= @2010-1-1 and $this <= @2020-1-1":"@2010-1-1T00:00:00"
  },
  "otherValues":"redact"
}
```

## Date-shift algorithm
You can specify dateShift as a de-identification method in the configuration file. With this method, the input date/dateTime/instant value will be shifted within a 100-day differential. The following algorithm is used to shift the target dates:

### Input
- [Required] A date/dateTime/instant value
- [Optional] _dateShiftKey_. If not specified, a randomly generated string will be used as default key.
- [Optional] _dateShiftScope_. If not specified, _resource_ will be set as default scope.

### Output
* A shifted date/datetime/instant value

### Steps
1. Get _dateShiftKeyPrefix_ according to _dateShiftScope_.
- For scope _resource_, _dateShiftKeyPrefix_ refers to the resource id.
- For scope _file_, _dateShiftKeyPrefix_ refers to the file name.
- For scope _folder_, _dateShiftKeyPrefix_ refers to the root input folder name.
2. Create a string by combining _dateShiftKeyPrefix_ and _dateShiftKey_.
3. Feed the above string to hash function to get an integer between [-50, 50]. 
4. Use the above integer as the offset to shift the input date/dateTime/instant value.

### Note

1. If the input date/dateTime/instant value does not contain an exact day, for example dates with only a year ("yyyy") or only a year and month ("yyyy-MM"), the date cannot be shifted and redaction will be applied.
2. If the input date/dateTime/instant value is indicative of age over 89, it will be redacted (including year) according to HIPAA Safe Harbor Method.
3. If the input dateTime/instant value contains time, time will be redacted. Time zone will keep unchanged.

## Crypto-hash method
You can specify the crypto-hash method in the configuration file. We use HMAC-SHA256 algorithm, which outputs a Hex encoded representation of the hashed output (for example, ```a3c024f01cccb3b63457d848b0d2f89c1f744a3d```). If you want the anonymized output to be conformant to the FHIR specification, use Crypto-hash on only those fields that can take a Hex encoded string of 64 bytes length.

A typical scenario is to replace resource ids across FHIR resources via crypto hashing. With a specific hash key, same resource ids that reside in resources and references will be hashed to a same value. There is a special case when crypto hashing a [literal reference](https://www.hl7.org/fhir/references.html#literal) element. The tool captures and transforms only the id part from a reference, for example, reference ```Patient/123``` will be hashed to ```Patient/a3c024f01cccb3b63457d848b0d2f89c1f744a3d```. In this way, you can easily resolve references across anonymized FHIR resources.

## Encrypt method
We use AES-CBC algorithm to transform FHIR data with an encryption key, and then replace the original value with a Base64 encoded representation of the encrypted value.
1. The encryption key needs to be exactly 128, 192 or 256 bits long.
2. The algorithm will generate a random and unique initialization vector (IV) for each encryption, therefore the encrypted results are different for the same input values.
3. If you want the anonymized output to be conformant to the FHIR specification, do use encrypt method on those fields that accept a Base64 encoded value. Besides, avoid encrypting data fields with length limits because the Base64 encoded value will be longer than the original value.

## Substitute method
You can specify a fixed, valid value to replace a target FHIR field. For example, for postal code, you can provide "12233". For birth date, you can provide '1990-01-01', etc.

For complex data types, you can provide a fixed json fragment following the [sample rules](#Sample-rules-using-FHIRPath).
You should provide valid value for the target data type to avoid unexpected errors.

## Perturb method
With perturbation rule, you can replace specific values with equally specific, but different values. You can choose to add random noise from a fixed range or a proportional range. In the [age example](#Sample-rules-using-FHIRPath) above, for a fixed range ```[-3, 3]```, every age is within +/- 3 years of the original value. For a proportional range ```[-0.1*originalAge, 0.1*originalAge]```, every age is within +/- 10% years of the original value. 

There are a few parameters that can help you customize the noise amount for different FHIR types.
- [required] **span** A non-negative value representing the random noise range. For *fixed* range type, the noise will be sampled from a uniform distribution over ```[-span/2, span/2]```. For *proportional* range type, the noise will be sampled from a uniform distribution over ```[-span/2 * value, span/2 * value]```. 
- [optional] **rangeType** Define whether the *span* value is *fixed* or *proportional*. The default value is *fixed*. 
- [optional] **roundTo** A value from 0 to 28 that specifies the number of decimal places to round to. The default value is *0* for integer types and *2* for decimal types. 

Note that the target field should be of either a numeric type (integer, decimal, unsignedInt, positiveInt) or a quantity type (Quantity, SimpleQuantity, Money, etc.). 

## Generalize method
As one of the anonymization methods, generalization means mapping values to the higher level of generalization. It is the process of abstracting distinguishing value into a more general, less distinguishing value. Generalization attempts to preserve data utility while also reducing the identifiability of the data. 
Generalization uses FHIRPath predicate expression to define a set of cases that specify the condition and target value like [sample rules](#Sample-rules-using-FHIRPath). Follows are some examples of cases.

|Data Type|Cases|Explanation|Input data-> Output data|
|-----|-----|-----|-----|
|numeric|_"$this>=0 and $this<20": "20"_|Data fall in the range [0,20) will be replaced with 20. |18 -> 20|
|numeric|_"true": "($this div 10)*10"_|Approximate data to multiples of 10. |18 -> 10|
|string| _"$this in ('es-AR' \| 'es-ES' \| 'es-UY')": "'es'"_|Data fall in the value set will be mapped to "es".|'es-UY' -> 'es'|
|string| _"$this.startsWith(\'123\')": "$this.subString(0,2)+\'*\*\*\*\' "_ |Mask sensitive string code.|'1230005' -> '123****'|
|date, dateTime, time|_"$this >= @2010-1-1": "@2010"_|Data fall in a date/time/dateTime range will be mapped to one date/time/dateTime value.| 2016-03-10 -> 2010|
|date, dateTime, time|_"$this.replaceMatches('(?&lt;year&gt;\\\d{2,4})-(?&lt;month&gt;\\\d{1,2})-(?&lt;day&gt;\\\d{1,2})\\\b', '${year}-${month}'"_|Omit "day" to generalize specific date.|2016-01-01 -> 2016-01|

For each generalization rule, there are several additional settings to specify in configuration files:
- [required] **cases** An object defining key-value pairs to specify case condition and replacement value using FHIRPath predicate expression. _key_ represents case condition and _value_ represents target value.

- [optional] **otherValues** Define the operation for values that do not match any of the cases. The value could be "redact" or "keep". The default value is "redact".

Since the output of FHIR expression is flexible, users should provide expressions with valid output value to avoid unexcepted errors.
## Current limitations
1. We support FHIR data in R4 and STU 3, JSON format. Support for XML is planned.
2. De-identification of fields within Extensions is not supported. 

## FAQ

### How to perform de-identified $export operation on the FHIR server?
De-identified export is an extension of the standard FHIR $export operation that takes de-identification config details as additional parameters. Here are the steps to enable and use de-identified export:

#### Configuration
1. Ensure that $export is [configured](https://github.com/microsoft/fhir-server/blob/master/docs/BulkExport.md) on the FHIR server. Take a note of the blob account that is configured as export location.
2. Go to the configuration page of the FHIR server App service on Azure portal and add new application setting with name **FhirServer:Features:SupportsAnonymizedExport** and set its value to **True**.
3. Save the configuration and restart the App service.

#### Usage
1. Create container named **anonymization** in the blob account that is configured as export location. Put your [anonymization config](#configuration-file-format) file in this container. You can also use the sample [HIPAA Safe Harbor config file](sample-configuration-file-for-hipaa-safe-harbor-method).
2. Note the Etag of the config file in the blob store. You can see the Etag in the properties dialog of the blob in the Azure Storage Explorer or at Azure portal.
3. Call the $export method on your FHIR server using the following URL pattern. It is an asynchronous call that returns HTTP 202 on success, and _content-location_ in header.

**{FHIR service base URL}/$export?_container={container name}&_anonymizationConfig={config file name}&_anonymizationConfigEtag="{ETag of config file}"**

here, _\_container_ is the name of the target container within the blob account where you want the data to be exported. The container name should follow the rules [here](https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata#container-names).

4. Go to the _content-location_ to check the status of the export. Once completed, the _content-location_ URL provides the URLs of the exported resources.


### How can we use FHIR Tools for Anonymization to anonymize HL7 v2.x data
You can build a pipeline to use [FHIR converter](https://github.com/microsoft/FHIR-Converter) to convert HL7 v2.x data to FHIR format, and subsequently use FHIR Tools for Anonymization to anonymize your data. 

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
