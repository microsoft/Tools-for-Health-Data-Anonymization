# Script to run custom activity executable
$AppFolder = "AdfApplication"
Expand-Archive -Path "$AppFolder.zip" -DestinationPath .
.\Fhir.Anonymizer.AzureDataFactoryPipeline.exe -f
if ($LastExitCode -ne 0) 
{
    Write-Host "An error occurred."
    exit 1
}