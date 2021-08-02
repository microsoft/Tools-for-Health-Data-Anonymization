<#
.SYNOPSIS
    Deploy Azure Data Factory Pipeline
.DESCRIPTION
    Deploys the FHIR De-Identification tool to Azure Data Factory. 
    If data factory or batch service exists, it will use the configured resource and just run the pipeline.
.PARAMETER ConfigFile
    Default: AzureDataFactorySettings.json
    settings to configure:
    - dataFactoryName - name for the data factory you are creating / using
    - resourceLocation - resource group you are creating / using
    - sourceStorageAccountName - name of azure storage account that contains source data and activity application folder.
    - sourceStorageAccountKey - key of azure storage account that contains source data
    - destinationStorageAccountName - name of azure storage that contains output container
    - destinationStorageAccountKey - key of azure storage that contains output container
    - sourceContainerName - container name of source data
    - destinationContainerName - container name of destination output
    - activityContainerName - container name for activity application
.PARAMETER SubscriptionId
    Default: use default subscription from your azure account.
.PARAMETER RunPipelineOnly
    Default: false
    If you have created the Azure Data Factory pipeline before with this script and you just want to Run data transform, 
    you can specify this option.
.PARAMETER BatchAccountName
    Default: Empty
    Specify to use existing Azure Batch Account. Will create new batch accout if not existed
.PARAMETER BatchAccountPoolName
    Default: Empty
    Specify which pool to use in Azure Batch Account. Will create new batch pool if not existed
.PARAMETER BatchComputeNodeSize
    Default: Standard_d1
    Specify the compute node size to allocate
.PARAMETER BatchComputeNodeRuntimeId
    Default: win10-x64
    Specify the dotnet runtime id in your compute node.
#>

[cmdletbinding()]
param(
    [string]$ConfigFile = "..\..\Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline\AzureDataFactorySettings.json",
    [Parameter(Mandatory=$true)]
    [string]$BatchAccountName,
    [Parameter(Mandatory=$true)]
    [string]$BatchAccountPoolName,
    [string]$SubscriptionId,
    [string]$ResourceGroupName,
    [switch]$RunPipelineOnly = $false,
    [string]$BatchComputeNodeSize = "Standard_d1",
    [string]$BatchComputeNodeRuntimeId = "win10-x64"
)

$fhirVersion = "R4"

cd ../Microsoft.Health.Fhir.Anonymizer.Shared.AzureDataFactoryPipeline/scripts
if ($RunPipelineOnly) 
{
	.\AzureDataFactoryPipelineUtility.ps1 -SubscriptionId $SubscriptionId -BatchAccountName $BatchAccountName -BatchAccountPoolName $BatchAccountPoolName -ResourceGroupName $ResourceGroupName -FhirVersion $fhirVersion -ConfigFile $ConfigFile -RunPipelineOnly -BatchComputeNodeSize $BatchComputeNodeSize -BatchComputeNodeRuntimeId $BatchComputeNodeRuntimeId
}
else {
	.\AzureDataFactoryPipelineUtility.ps1 -SubscriptionId $SubscriptionId -BatchAccountName $BatchAccountName -BatchAccountPoolName $BatchAccountPoolName -ResourceGroupName $ResourceGroupName -FhirVersion $fhirVersion -ConfigFile $ConfigFile -BatchComputeNodeSize $BatchComputeNodeSize -BatchComputeNodeRuntimeId $BatchComputeNodeRuntimeId
}