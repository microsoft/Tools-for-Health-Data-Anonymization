# Multi-Target Build Script for Health Data Anonymization Tools
# This script allows building specific .NET Framework versions

param(
    [string]$Framework = "all",  # net8.0, net9.0, or all
    [string]$Configuration = "Release",
    [string]$Project = "all",  # DICOM, FHIR, or all
    [switch]$Clean,     # Clean the output directories
    [switch]$Restore,   # Restore NuGet packages
    [switch]$Test,      # Run tests
    [switch]$Pack       # Pack the project
)

$SupportedFrameworks = @("net8.0", "net9.0")
$RootPath = $PSScriptRoot

# Define project configurations to eliminate duplication
$Projects = @{
    "DICOM" = @{
        SolutionPath = "$RootPath\DICOM\Dicom.Anonymizer.sln"
        DisplayName = "DICOM Anonymizer"
    }
    "FHIR" = @{
        SolutionPath = "$RootPath\FHIR\Fhir.Anonymizer.sln"
        DisplayName = "FHIR Anonymizer"
    }
}

# Function to check if a .NET framework is available
function Test-DotNetFramework {
    param([string]$Framework)
    try {
        $version = $Framework.Substring(3)  # Remove "net" prefix
        $output = dotnet --list-runtimes 2>$null | Where-Object { $_ -like "*Microsoft.NETCore.App $version*" }
        return $output -ne $null
    }
    catch {
        return $false
    }
}

Write-Host "Health Data Anonymization Tools - Multi-Target Build Script" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if ($Project -eq "all" -or $Project -eq "DICOM") {
        dotnet clean $Projects["DICOM"].SolutionPath -c $Configuration
    }
    if ($Project -eq "all" -or $Project -eq "FHIR") {
        dotnet clean $Projects["FHIR"].SolutionPath -c $Configuration
    }
    Write-Host "Clean completed!" -ForegroundColor Green
    
    # If only clean was requested (no other operations), exit here
    if (-not $Restore -and -not $Test -and -not $Pack) {
        Write-Host "Clean operation completed. Use -Restore, -Test, or -Pack to perform additional operations." -ForegroundColor Yellow
        exit 0
    }
}

# Restore packages if requested
if ($Restore) {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    if ($Project -eq "all" -or $Project -eq "DICOM") {
        dotnet restore $Projects["DICOM"].SolutionPath
    }
    if ($Project -eq "all" -or $Project -eq "FHIR") {
        dotnet restore $Projects["FHIR"].SolutionPath
    }
}

# Build function
function Build-Project {
    param(
        [string]$SolutionPath,
        [string]$ProjectName,
        [string]$TargetFramework
    )
    
    Write-Host "Building $ProjectName for $TargetFramework..." -ForegroundColor Cyan
    
    # Determine if we should skip restore (if we already restored explicitly)
    $restoreFlag = if ($Restore) { "--no-restore" } else { "" }
    
    if ($TargetFramework -eq "all") {
        $buildCmd = "dotnet build `"$SolutionPath`" -c $Configuration --maxcpucount:1 $restoreFlag".Trim()
    } else {
        $buildCmd = "dotnet build `"$SolutionPath`" -c $Configuration -f $TargetFramework --maxcpucount:1 $restoreFlag".Trim()
    }
    
    Write-Host "Executing: $buildCmd" -ForegroundColor Gray
    Invoke-Expression $buildCmd
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $ProjectName ($TargetFramework)"
        exit 1
    }
}

# Test function
function Test-Project {
    param(
        [string]$SolutionPath,
        [string]$ProjectName,
        [string]$TargetFramework
    )
    
    Write-Host "Running tests for $ProjectName ($TargetFramework)..." -ForegroundColor Cyan
    
    if ($TargetFramework -eq "all") {
        $testCmd = "dotnet test `"$SolutionPath`" -c $Configuration --no-build"
    } else {
        $testCmd = "dotnet test `"$SolutionPath`" -c $Configuration -f $TargetFramework --no-build"
    }
    
    Write-Host "Executing: $testCmd" -ForegroundColor Gray
    Invoke-Expression $testCmd
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Some tests failed for $ProjectName ($TargetFramework)"
    }
}

# Pack function
function Pack-Project {
    param(
        [string]$SolutionPath,
        [string]$ProjectName,
        [string]$TargetFramework
    )
    
    Write-Host "Creating packages for $ProjectName ($TargetFramework)..." -ForegroundColor Cyan
    
    # Pack command doesn't support -f parameter, it packs all target frameworks by default
    $packCmd = "dotnet pack `"$SolutionPath`" -c $Configuration --no-build -o `"$RootPath\packages`""
    
    Write-Host "Executing: $packCmd" -ForegroundColor Gray
    Invoke-Expression $packCmd
}

# Determine frameworks to build
$FrameworksToBuild = @()
if ($Framework -eq "all") {
    if ($Test) {
        # For testing, we need to test each framework individually to avoid conflicts
        $FrameworksToBuild = $SupportedFrameworks
    } else {
        # For building/packing, we can build all frameworks in one pass
        $FrameworksToBuild = @("all")
    }
} elseif ($Framework -in $SupportedFrameworks) {
    # Check if the specified framework is actually available
    if (-not (Test-DotNetFramework $Framework)) {
        Write-Error "ERROR: $Framework runtime is not installed on this system."
        Write-Host ""
        Write-Host "Available .NET runtimes on this system:" -ForegroundColor Yellow
        dotnet --list-runtimes | Where-Object { $_ -like "*Microsoft.NETCore.App*" }
        Write-Host ""
        Write-Host "To install $Framework, download from:" -ForegroundColor Cyan
        Write-Host "https://dotnet.microsoft.com/download/dotnet/" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Alternatively, use one of the available frameworks:" -ForegroundColor Yellow
        Write-Host "  .\build.ps1 -Framework net8.0" -ForegroundColor Gray
        Write-Host "  .\build.ps1 -Framework net9.0" -ForegroundColor Gray
        exit 1
    }
    $FrameworksToBuild = @($Framework)
} else {
    Write-Error "Unsupported framework: $Framework. Supported frameworks: $($SupportedFrameworks -join ', '), all"
    exit 1
}

# Get list of projects to build
$projectsToProcess = @()
if ($Project -eq "all") {
    $projectsToProcess = @("DICOM", "FHIR")
} else {
    $projectsToProcess = @($Project)
}

# Build projects
foreach ($fw in $FrameworksToBuild) {
    Write-Host "`nBuilding for framework: $fw" -ForegroundColor Yellow
    
    foreach ($proj in $projectsToProcess) {
        $projectConfig = $Projects[$proj]
        $solutionPath = $projectConfig.SolutionPath
        $displayName = $projectConfig.DisplayName
        
        Build-Project $solutionPath $displayName $fw
        
        if ($Test) {
            Test-Project $solutionPath $displayName $fw
        }
        
        if ($Pack) {
            Pack-Project $solutionPath $displayName $fw
        }
    }
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "`nUsage Examples:" -ForegroundColor Yellow
Write-Host "  .\build.ps1 -Framework net8.0 -Project DICOM" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Framework net9.0 -Clean -Restore -Test" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Framework all -Pack" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Framework net8.0 -Configuration Debug" -ForegroundColor Gray
