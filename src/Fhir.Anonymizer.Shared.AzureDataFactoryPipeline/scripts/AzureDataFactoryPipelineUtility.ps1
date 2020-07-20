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
.PARAMETER FhirVersion
    Specify the FHIR version (R4 or Stu3)
#>

[cmdletbinding()]
param(
    [string]$ConfigFile = "AzureDataFactorySettings.json",
    [Parameter(Mandatory=$true)]
    [string]$BatchAccountName,
    [Parameter(Mandatory=$true)]
    [string]$BatchAccountPoolName,
    [string]$SubscriptionId,
    [string]$ResourceGroupName,
    [switch]$RunPipelineOnly = $false,
    [string]$BatchComputeNodeSize = "Standard_d1",
    [string]$BatchComputeNodeRuntimeId = "win10-x64",
    [Parameter(Mandatory=$true)]
    [string]$FhirVersion
)

function BuildToolAndUploadToBlobContainer 
{
    param ($storageAccountName, $storageAccountKey, $containerName, $AppFolder, $AppName, $dotnetRuntimeId)
    # Build AzureDataFactory Custom Activity Tool
    New-Item -ItemType Directory -Force -Path "Build"

    dotnet publish -c Release -r $dotnetRuntimeId --self-contained true -o "Build\$AppFolder" "..\..\$AppName\$AppName.csproj" 
    Compress-Archive -Path "Build\$AppFolder\*" -DestinationPath "Build\$AppFolder.zip" -Force

    $currentDirectory = $(Get-Location).Path    

    # Get storage context, exit on fail.
    try 
    {
        $storageContext = New-AzStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageAccountKey -ErrorAction Stop
    }
    catch
    {
        Write-Host "Connect to Azure Storage $storageAccountName failed. Please make sure the account name and account key are correct."
        throw
    }

    try 
    {
        Get-AzStorageContainer -Name $containerName -Context $storageContext -ErrorAction Stop
        Write-Host "Using existing container '$containerName' to upload custom activity application" -ForegroundColor Green
    }
    catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceNotFoundException] 
    {
        Write-Host "Creating container $containerName..."
        New-AzStorageContainer -Name $containerName -Context $storageContext -Permission Off
    }

    Set-AzStorageBlobContent -Context $storageContext -Container $containerName `
        -File "$currentDirectory/Build/$AppFolder.zip" -Blob "$AppFolder/$AppFolder.zip" -Force 
    Set-AzStorageBlobContent -Context $storageContext -Container $containerName `
        -File "$currentDirectory/CustomActivity.ps1" -Blob "$AppFolder/CustomActivity.ps1" -Force 
}

function CheckAndCreateAzureBatchLinkedServiceAndComputeEnvrionment
{
    param ($resourceGroupName, $location, $batchName, $batchPoolName, $computeNodeSize)

    # Get or create azure batch if not exist
    $prepareBatchAccountTimestamp = Get-Date
    $prepareBatchAccountTimeoutInMinutes = 25
    while ($True) 
    {
        if ($prepareBatchAccountTimestamp.AddMinutes($prepareBatchAccountTimeoutInMinutes) -lt (Get-Date)) 
        {
            throw "Batch account '$batchName' not ready in 25 minutes. Please check the account status."
        }

        try
        {
            $batchContext = Get-AzBatchAccount -AccountName $batchName -ErrorAction Stop
            Write-Host "Using existing batch service '$($batchContext.TaskTenantUrl)'" -ForegroundColor Green
        }
        catch 
        {
            if (-Not $_.Exception.Message.StartsWith("ResourceNotFound")) 
            {
                Write-Host $_.ToString()
                throw
            }

            New-AzBatchAccount -AccountName $batchName -ResourceGroupName $resourceGroupName -Location $location -ErrorAction Stop
            $batchContext = Get-AzBatchAccount -AccountName $batchName
        }

        if ($batchContext) 
        {
            break
        }
        else 
        {
            Start-Sleep -Seconds 60
        }
    }

    # Get or create azure batch pool and nodes if not exist 
    try 
    {
        $pool = Get-AzBatchPool -Id $batchPoolName -BatchContext $batchContext -ErrorAction Stop
        Write-Host "Using existing Azure Batch Pool '$($pool.Id)'." -ForegroundColor Green
    }
    catch
    {
        if (-Not ($_.Exception.Message -Match 'PoolNotFound'))
        {
            Write-Host $_.ToString()
            throw
        }

        $imageReference = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSImageReference" -ArgumentList @("windowsserver", "microsoftwindowsserver", "2016-datacenter", "latest")
        $configuration = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSVirtualMachineConfiguration" -ArgumentList @($imageReference, "batch.node.windows amd64")

        New-AzBatchPool -Id $batchPoolName -VirtualMachineSize $computeNodeSize `
            -BatchContext $batchContext -VirtualMachineConfiguration $configuration `
            -TargetDedicatedComputeNodes 1 -ErrorAction Stop
        
        Write-Host "Running first boot for new compute node. This may takes up to 20 minutes..."
        $nodeBootTimestamp = Get-Date
        while ($True) 
        {
            $node = Get-AzBatchComputeNode -PoolId $batchPoolName -BatchContext $batchContext
            if ($nodeBootTimestamp.AddMinutes(25) -lt (Get-Date)) 
            {
                throw "Compute node start timeout. Please check the node status."
            }
            $timestamp = Get-Date -Format "[yyyy-MM-dd hh:mm:ss]"
            if ($node) 
            {
                if ($node.State -eq "Idle") 
                {
                    Write-Host "$timestamp Initialized batch service compute node '$($node.Url)'" -ForegroundColor Green
                    break
                }
                elseif ($node.State -eq "Starting") 
                {
                    Write-Host "$timestamp Compute node state: Starting node"
                }
                else 
                {
                    Write-Host "$timestamp Compute node state: Allocating resource"
                }
            }
            else 
            {
                Write-Host "$timestamp Compute node state: Allocating resource"
            } 
            Start-Sleep -Seconds 60
        }
        Write-Host "Created new Azure Batch Pool $($pool.Id)." -ForegroundColor Green
    }
}

function CreateAzureDataFactoryAndPipeline
{
    param ($resourceGroupName, $userConfig, $batchName, $batchPoolName)
    $batchContext = Get-AzBatchAccountKey -AccountName $batchName
    
    # Write template parameter file
    $json = Get-Content -Raw -Path "./ArmTemplate/arm_template_parameters.json" | ConvertFrom-Json
    $json.parameters.location.value = $userConfig.resourceLocation
    $json.parameters.factoryName.value = $userConfig.dataFactoryName
    $json.parameters.sourceStorageLinkedService_connectionString.value = "DefaultEndpointsProtocol=https;AccountName=$($userConfig.sourceStorageAccountName);AccountKey=$($userConfig.sourceStorageAccountKey);EndpointSuffix=core.windows.net"
    $json.parameters.sourceStorageContainerName.value = $userConfig.sourceStorageContainerName.ToLower()
    $json.parameters.sourceContainerFolderPath.value = $userConfig.sourceContainerFolderPath
    $json.parameters.sourceStorageActivityApplicationContainer.value = $userConfig.activityContainerName.ToLower()
    $json.parameters.destinationStorageLinkedService_connectionString.value = "DefaultEndpointsProtocol=https;AccountName=$($userConfig.destinationStorageAccountName);AccountKey=$($userConfig.destinationStorageAccountKey);EndpointSuffix=core.windows.net"
    $json.parameters.destinationStorageContainerName.value = $userConfig.destinationStorageContainerName.ToLower()
    $json.parameters.destinationContainerFolderPath.value = $userConfig.destinationContainerFolderPath
    $json.parameters.azureBatchLinkedService_accessKey.value = $batchContext.PrimaryAccountKey
    $json.parameters.azureBatchLinkedService_poolName.value = $batchPoolName
    $json.parameters.azureBatchLinkedService_properties_typeProperties_accountName.value = $batchContext.AccountName
    $json.parameters.azureBatchLinkedService_properties_typeProperties_batchUri.value = $batchContext.TaskTenantUrl
    $json.parameters.fhirVersion.value = $FhirVersion
    ConvertTo-Json $json -Depth 10 | Set-Content "./ArmTemplate/arm_template_parameters.json"

    # Data Factory settings are overwritten in every Deployment/Execution to make sure input/output/application settings are newest 
    Write-Host "Deploying Data Factory $($userConfig.dataFactoryName)..."
    New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName `
        -TemplateFile ./ArmTemplate/arm_template.json -TemplateParameterFile ./ArmTemplate/arm_template_parameters.json
    Write-Host "Azure Data Factory $($userConfig.dataFactoryName) created/updated." -ForegroundColor Green   
}

function RunAzureDataFactoryPipeline
{
    param ($resourceGroupName, $dataFactoryName, $adfPipelineName)
    
    $runStartTimestamp  = Get-Date
    $runId = Invoke-AzDataFactoryV2Pipeline -DataFactoryName $dataFactoryName `
        -ResourceGroupName $resourceGroupName -PipelineName $adfPipelineName 

    while ($True) 
    {
        $run = Get-AzDataFactoryV2PipelineRun -ResourceGroupName $resourceGroupName `
            -DataFactoryName $dataFactoryName -PipelineRunId $runId

        if ($run)
        {
            $timestamp = Get-Date -Format "[yyyy-MM-dd hh:mm:ss]"
            if ($run.Status -eq 'Succeeded') 
            {
                Write-Host ("$timestamp Pipeline run $($run.Status)!") -ForegroundColor Green
                $run
                break
            }
            elseif ($run.Status -eq 'Failed') 
            {
                Write-Host ("$timestamp Pipeline run $($run.Status)!") -ForegroundColor Red
                $run
                break
            }
            Write-Host "$timestamp Pipeline is running...status: InProgress"
        }

        Start-Sleep -Seconds 60
    }

    $result = Get-AzDataFactoryV2ActivityRun -DataFactoryName $dataFactoryName -ResourceGroupName $resourceGroupName -PipelineRunId $runId -RunStartedAfter $runStartTimestamp -RunStartedBefore (Get-Date)
    Write-Host "Activity 'Output' section:"
    $result.Output -join "`r`n"
}

$supportedVersion="stu3","r4"
# Check App version, case insensititve
if ($FhirVersion -notin $supportedVersion )
{
    throw "App Version is not supported"
}

# Check batch account parameter
if ($BatchAccountName -ne "" -And $BatchAccountPoolName -eq "") 
{
    throw "BatchAccountPoolName must be specified with BatchAccountName"
}

# Load user Config
$userConfig = Get-Content -Raw -Path $ConfigFile | ConvertFrom-Json

if ($ResourceGroupName -eq "") 
{
    # generate default resource group name if customer not specified.
    $ResourceGroupName = "$($userConfig.dataFactoryName)resourcegroup"
}

# Optional, select Azure Subscription by Id if you don't want to use your default subscription
if ($SubscriptionId)
{
    Get-AzSubscription -SubscriptionId $SubscriptionId | Set-AzContext
}

Write-Host "Use resource group: $ResourceGroupName"
Get-AzResourceGroup -Name $ResourceGroupName -ErrorVariable notPresent -ErrorAction SilentlyContinue
if ($notPresent)
{
    Write-Host "Resource Group $ResourceGroupName not exist. Creating."
    New-AzResourceGroup -Name $ResourceGroupName -Location $userConfig.resourceLocation -ErrorAction Stop
}
else 
{
    Write-Host "Resource Group $ResourceGroupName already exist."
}

$appName="Fhir.Anonymizer.$FhirVersion.AzureDataFactoryPipeline"
$appFolder = "$FhirVersion.AdfApplication"
$adfPipelineName = "AdfAnonymizerPipeline"

if (!$RunPipelineOnly) 
{
    BuildToolAndUploadToBlobContainer $userConfig.destinationStorageAccountName $userConfig.destinationStorageAccountKey $userConfig.activityContainerName.ToLower() $appFolder $appName $BatchComputeNodeRuntimeId

    CheckAndCreateAzureBatchLinkedServiceAndComputeEnvrionment $ResourceGroupName $userConfig.resourceLocation $BatchAccountName $BatchAccountPoolName $BatchComputeNodeSize
    
    CreateAzureDataFactoryAndPipeline $ResourceGroupName $userConfig $BatchAccountName $BatchAccountPoolName
}

RunAzureDataFactoryPipeline $ResourceGroupName $userConfig.dataFactoryName $adfPipelineName
