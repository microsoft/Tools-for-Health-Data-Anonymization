<#
.PARAMETER FhirVersion
    Specify FHIR version.
#>

[cmdletbinding()]
param(
    [string]$FhirVersion
)

# Script to run custom activity executable
$AppFolder = "$FhirVersion.AdfApplication"
Expand-Archive -Path "$AppFolder.zip" -DestinationPath .
&".\Fhir.Anonymizer.$FhirVersion.AzureDataFactoryPipeline.exe" -f
if ($LastExitCode -ne 0) 
{
    Write-Host "An error occurred."
    exit 1
}