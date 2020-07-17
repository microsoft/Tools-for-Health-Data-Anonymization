<#
.PARAMETER AppVersion
    Specify the version of app.
#>

[cmdletbinding()]
param(
    [string]$AppVersion
)

# Script to run custom activity executable
$AppFolder = "$AppVersion.AdfApplication"
Expand-Archive -Path "$AppFolder.zip" -DestinationPath .
&".\Fhir.Anonymizer.$AppVersion.AzureDataFactoryPipeline.exe" -f
if ($LastExitCode -ne 0) 
{
    Write-Host "An error occurred."
    exit 1
}