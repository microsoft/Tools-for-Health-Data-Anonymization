# Multi-Target Build Support

This project now supports building for multiple .NET Framework versions: .NET 6.0, 7.0 and 8.0.

## Quick Start

### Using PowerShell Script (Recommended)

```powershell
# Build all projects for all supported frameworks
.\build.ps1

# Build specific framework
.\build.ps1 -Framework net8.0

# Build specific project (DICOM or FHIR)
.\build.ps1 -Project DICOM -Framework net6.0

# Clean, restore, build, and test
.\build.ps1 -Clean -Restore -Test -Framework net6.0

# Create NuGet packages (requires building all frameworks first)
.\build.ps1 -Pack -Framework all

# Complete release workflow: Clean, restore, build all frameworks, test, and pack
.\build.ps1 -Clean -Restore -Test -Pack -Framework all
```

### Using Batch File

```cmd
# Build all projects for all frameworks
build.bat

# Build specific framework and project
build.bat net6.0 DICOM Release
build.bat net8.0 FHIR Debug
```

### Using .NET CLI Directly

```bash
# Build for all target frameworks
dotnet build DICOM/Dicom.Anonymizer.sln

# Build for specific framework
dotnet build DICOM/Dicom.Anonymizer.sln -f net6.0
dotnet build FHIR/Fhir.Anonymizer.sln -f net8.0

# Test specific framework
dotnet test DICOM/Dicom.Anonymizer.sln -f net6.0
```

## Supported Frameworks

- **.NET 6.0** (`net6.0`) - LTS version, stable and widely supported
- **.NET 7.0** (`net7.0`) - Latest features and performance improvements
- **.NET 8.0** (`net8.0`) - Latest LTS version with newest features

## Project Structure

### Library Projects (Multi-Targeted)
These projects build for all supported frameworks:
- `Microsoft.Health.Anonymizer.Common`
- `Microsoft.Health.Dicom.Anonymizer.Core`
- `Microsoft.Health.Fhir.Anonymizer.R4.Core`
- `Microsoft.Health.Fhir.Anonymizer.Stu3.Core`

### Executable Projects (Single-Targeted)
Command-line tools currently target .NET 8.0 by default:
- `Microsoft.Health.Dicom.Anonymizer.CommandLineTool`
- `Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool`
- `Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool`

## Configuration Files

### Directory.Build.props (Root)

- **Central configuration hub** that imports `Framework.props`
- Defines `SupportedTargetFrameworks` globally (net6.0;net7.0;net8.0)
- Common properties for all projects (Nullable, ImplicitUsings, TreatWarningsAsErrors)
- Package metadata for NuGet packages (license, copyright, version)
- Framework-specific conditional compilation symbols (NET6_0_OR_GREATER, etc.)

### DICOM/Directory.Build.props

- DICOM-specific build settings
- StyleCop configuration
- Inherits from root Directory.Build.props

### FHIR/Directory.Build.props

- FHIR-specific build settings
- Inherits from root Directory.Build.props

### Framework.props

- Framework-specific package version management
- Compatibility configurations

## Framework-Specific Features

### .NET 6.0
- Long Term Support (LTS) release
- Full compatibility with all project types
- **Note**: Some newer packages (9.0.x) show compatibility warnings but work correctly

### .NET 7.0
- **Warning**: End-of-life framework (NETSDK1138)
- Included for compatibility testing
- **Note**: Microsoft.NET.Test.Sdk and newer packages show "untested" warnings but function correctly
- Test projects may show warnings about framework support - these are suppressed via `Framework.props`

### .NET 8.0
- Current LTS release
- Recommended for production use
- Full compatibility with all packages and tooling

## Common Warnings and Solutions

### Test SDK Warnings (.NET 6.0/7.0)
**Warning**: `Microsoft.NET.Test.Sdk doesn't support net7.0 and has not been tested with it`
**Solution**: These warnings are automatically suppressed via `Framework.props`. The Test SDK works correctly despite the warnings.

### Package Compatibility Warnings (.NET 6.0/7.0)
**Warning**: Various Microsoft packages show ".NET 7.0 is untested" warnings
**Solution**: These warnings are automatically suppressed and the packages function correctly.

### End-of-Life Warnings (.NET 7.0)
**Warning**: `NETSDK1138: The target framework 'net7.0' is out of support`
**Solution**: This is informational only. .NET 7.0 support is provided for compatibility testing.

You can use conditional compilation to target specific framework features:

```csharp
#if NET6_0_OR_GREATER
    // Code for .NET 6.0 and later
#elif NET8_0_OR_GREATER
    // Code for .NET 8.0 and later
#endif
```

## NuGet Package Support

Multi-targeted library projects will automatically create NuGet packages that support all target frameworks, allowing consumers to use the appropriate version for their project.

### Creating NuGet Packages

**Important**: To create NuGet packages, you must build ALL target frameworks first, as `dotnet pack` includes all frameworks defined in the project file.

```powershell
# ✅ Correct: Build all frameworks, then pack
.\build.ps1 -Framework all -Pack

# ❌ Incorrect: This will fail because pack expects all frameworks to be built
.\build.ps1 -Framework net8.0 -Pack

# Complete release workflow: Clean, restore, build all frameworks, test, and pack
.\build.ps1 -Clean -Restore -Test -Pack -Framework all

# Create packages for specific project only  
.\build.ps1 -Framework all -Project DICOM -Pack
.\build.ps1 -Framework all -Project FHIR -Pack
```

### What Gets Packed

Only library projects with `<IsPackable>true</IsPackable>` create NuGet packages:
- ✅ `Microsoft.Health.Anonymizer.Common` → `Microsoft.Health.Anonymizer.Common.1.0.0.nupkg`
- ✅ `Microsoft.Health.Dicom.Anonymizer.Core` → `Microsoft.Health.Dicom.Anonymizer.Core.1.0.0.nupkg`  
- ✅ `Microsoft.Health.Fhir.Anonymizer.R4.Core` → `Microsoft.Health.Fhir.Anonymizer.R4.Core.1.0.0.nupkg`
- ✅ `Microsoft.Health.Fhir.Anonymizer.Stu3.Core` → `Microsoft.Health.Fhir.Anonymizer.Stu3.Core.1.0.0.nupkg`
- ❌ Test projects (not packable)
- ❌ Command-line tools (not packable)

### Package Contents

Each `.nupkg` file contains:
- **All target framework versions** (net6.0, net7.0, net8.0 DLLs)
- **Dependencies information** (package references)
- **Metadata** (version, license, description)
- **Documentation** (XML docs if available)

### Using the Packages

Once created, other projects can reference these packages:

```xml
<PackageReference Include="Microsoft.Health.Fhir.Anonymizer.R4.Core" Version="1.0.0" />
<PackageReference Include="Microsoft.Health.Dicom.Anonymizer.Core" Version="1.0.0" />
```

The NuGet system automatically selects the appropriate framework version based on the consuming project's target framework.

## Switching Between Single and Multi-Targeting

### To enable multi-targeting for a project

Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFrameworks>$(SupportedTargetFrameworks)</TargetFrameworks>`

### To target only the latest framework

Change `<TargetFrameworks>$(SupportedTargetFrameworks)</TargetFrameworks>` to `<TargetFramework>net8.0</TargetFramework>`

## Troubleshooting

### Package Compatibility Issues
If you encounter package compatibility issues with specific frameworks, add framework-specific package references in `Framework.props`.

### Build Performance
Multi-targeting increases build time as each framework is built separately. For development, you can temporarily target a single framework.

### Testing
Ensure tests pass on all target frameworks, as behavior may differ between .NET versions.

### Pack Issues
**Error**: `The file 'Microsoft.Health.*.dll' to be packed was not found on disk`
**Cause**: Trying to pack without building all target frameworks first.
**Solution**: Always use `-Framework all` when packing:
```powershell
# ❌ This will fail
.\build.ps1 -Framework net8.0 -Pack

# ✅ This works  
.\build.ps1 -Framework all -Pack
```

## Examples

### Building and Testing DICOM Anonymizer for .NET 6.0

```powershell
.\build.ps1 -Project DICOM -Framework net6.0 -Test
```

### Development Build for .NET 8.0 Only

```bash
dotnet build -f net8.0 -c Debug
```

## Known Issues

### Build Issues Resolved
- **File locking issues**: Resolved by adding `--maxcpucount:1` to both build scripts to force single-threaded builds.
- **Assets file issues**: Resolved by running `dotnet restore` after updating target frameworks.

### Known Limitations
- **FHIR Test Projects**: Some test projects cannot build for .NET 6.0 due to incompatible test dependencies:
  - `Microsoft.NET.Test.Sdk 17.14.1` doesn't support .NET 6.0
  - Affected projects: Unit tests and functional tests in FHIR solution
  - **Workaround**: These test projects still build successfully for .NET 8.0
- **Package Warnings**: Some Microsoft.Extensions packages (9.0.x versions) show warnings when used with .NET 6.0, but builds succeed.

### Test Status by Framework
- ✅ **DICOM**: Builds successfully for .NET 6.0, 7.0, and 8.0 (all projects including tests)
- ✅ **FHIR Core Libraries**: Build successfully for .NET 6.0, 7.0, and 8.0
- ✅ **FHIR Command-line Tools**: Build successfully for .NET 6.0, 7.0, and 8.0
- ⚠️ **FHIR Test Projects**: May have compatibility issues with .NET 6.0/7.0 due to test dependencies
