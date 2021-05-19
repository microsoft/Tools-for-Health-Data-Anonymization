param(
        [Parameter(Mandatory)]
        [ValidateSet('short','medium','long')]
        [string]$JobType
    )

# ensure correct location
$startLocation = Get-Location
#Set-Location -Path c:/fhircli/benchmarks/src/Microsoft.Health.Fhir.Anonymizer.Stu3.Benchmarks/
Set-Location -Path C:\onecseweek21\itye\FHIR-Tools-for-Anonymization\src\Microsoft.Health.Fhir.Anonymizer.Stu3.Benchmarks

# execute benchmark
dotnet run -c Release -- --job $JobType --memory --runtimes netcoreapp3.1 --filter *FullFlow*

# Revert location
Set-Location -Path $startLocation