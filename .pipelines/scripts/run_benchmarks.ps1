
param(
    [Parameter()]
    [ValidateSet('dry','short','medium','long')]
    [string]
    $jobType = 'dry'

)

dotnet run -c Release -- --job $jobType --memory --runtimes netcoreapp3.1 --filter *FullFlow*