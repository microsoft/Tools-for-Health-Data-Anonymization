# Tools for Health Data Anonymization

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

Tools for Health Data Anonymization is a .NET 8.0 project that provides anonymization capabilities for healthcare data, specifically FHIR and DICOM formats. The project includes command-line tools, core libraries, unit tests, functional tests, and Azure Data Factory pipeline components.

## Working Effectively

### Prerequisites and Environment Setup
- Install .NET 8.0 SDK (confirmed working version: .NET 8.0.118)
- The project uses NuGet packages exclusively from nuget.org
- Code analysis is enforced using StyleCop with custom rules (stylecop.json and CustomAnalysisRules.ruleset)

### Build and Test Process

#### FHIR Components
Navigate to the FHIR directory for all FHIR-related operations:
```bash
cd FHIR
```

**Bootstrap and build (NEVER CANCEL - Build takes ~3 seconds, set timeout to 120+ seconds):**
```bash
dotnet restore    # Takes ~2 seconds
dotnet build --configuration Release    # Takes ~3 seconds, NEVER CANCEL
```

**Run tests (NEVER CANCEL - Tests take ~32 seconds, set timeout to 300+ seconds):**
```bash
dotnet test --configuration Release --collect "Code coverage"    # Takes ~32 seconds, NEVER CANCEL
```

#### DICOM Components
Navigate to the DICOM directory for all DICOM-related operations:
```bash
cd DICOM
```

**Bootstrap and build (NEVER CANCEL - Build takes ~2 seconds, set timeout to 120+ seconds):**
```bash
dotnet restore    # Takes ~1 second
dotnet build --configuration Release    # Takes ~2 seconds, NEVER CANCEL
```

**Run tests (NEVER CANCEL - Tests take ~10 seconds, set timeout to 180+ seconds):**
```bash
dotnet test --configuration Release --collect "Code coverage"    # Takes ~10 seconds, NEVER CANCEL
```

### Command-Line Tools Usage

#### FHIR R4 Anonymizer
**Location:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Release/net8.0/`
```bash
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool -i input_folder -o output_folder
```

#### FHIR STU3 Anonymizer
**Location:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool/bin/Release/net8.0/`
```bash
./Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool -i input_folder -o output_folder
```

#### DICOM Anonymizer
**Location:** `DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/bin/Release/net8.0/`
```bash
./Microsoft.Health.Dicom.Anonymizer.CommandLineTool -I input_folder -O output_folder
```

## Validation and Testing

### Manual Validation Requirements
**ALWAYS manually validate changes by running the appropriate anonymizer on sample data:**

**For FHIR changes:**
```bash
cd FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Release/net8.0/
mkdir -p /tmp/fhir-test-output
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool -i ../../../../../samples/fhir-r4-files -o /tmp/fhir-test-output -v
# Verify output files are created and contain anonymized data
ls -la /tmp/fhir-test-output/
```

**For DICOM changes:**
```bash
cd DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/bin/Release/net8.0/
mkdir -p /tmp/dicom-test-output
./Microsoft.Health.Dicom.Anonymizer.CommandLineTool -I ../../../../../samples -O /tmp/dicom-test-output
# Verify output files are created
ls -la /tmp/dicom-test-output/
```

### Known Test Issues
- Some unit tests fail due to missing test configuration files (expected behavior)
- Some functional tests fail due to missing configuration files (expected behavior)
- Azure Data Factory pipeline tests are skipped (expected behavior)
- Code coverage collection only works on Windows (expected on Linux)
- **DO NOT** attempt to fix these test failures unless specifically requested

### Pre-commit Validation
Always run these commands before committing changes:
```bash
cd FHIR && dotnet build --configuration Release
cd ../DICOM && dotnet build --configuration Release
```

## Project Structure and Key Locations

### FHIR Components
- **Core Libraries:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.{R4|Stu3}.Core/`
- **Command-Line Tools:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.{R4|Stu3}.CommandLineTool/`
- **Unit Tests:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.{R4|Stu3}.Core.UnitTests/`
- **Functional Tests:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.{R4|Stu3}.FunctionalTests/`
- **Azure Data Factory:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.{R4|Stu3}.AzureDataFactoryPipeline/`
- **Shared Code:** `FHIR/src/Microsoft.Health.Fhir.Anonymizer.Shared.*/`
- **Sample Files:** `FHIR/samples/fhir-r4-files/` and `FHIR/samples/fhir-stu3-files/`
- **Configuration:** `configuration-sample.json` (copied to build outputs)

### DICOM Components
- **Core Library:** `DICOM/src/Microsoft.Health.Dicom.Anonymizer.Core/`
- **Command-Line Tool:** `DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/`
- **Common Library:** `DICOM/src/Microsoft.Health.Anonymizer.Common/`
- **Unit Tests:** `DICOM/src/Microsoft.Health.Dicom.Anonymizer.*.UnitTests/`
- **Sample Files:** `DICOM/samples/` (I290.dcm, I341.dcm, lung.dcm)
- **Configuration:** `configuration.json` (default configuration file)

### Build and Configuration Files
- **Solutions:** `FHIR/Fhir.Anonymizer.sln` and `DICOM/Dicom.Anonymizer.sln`
- **Build Props:** `DICOM/Directory.Build.props` (StyleCop configuration)
- **Release Pipeline:** `release.yml` (Azure DevOps build pipeline)
- **NuGet:** `nuget.config` (package source configuration)

### Documentation
- **Main README:** `README.md` (overview and contributing guidelines)
- **FHIR Documentation:** `docs/FHIR-anonymization.md`
- **DICOM Documentation:** `docs/DICOM-anonymization.md`
- **DICOM Configuration:** `DICOM/Sample-Configuration-WithNote.md`

## Common Tasks and Workflows

### Adding New Anonymization Methods
1. Implement processor in `*/Core/Processors/` directory
2. Add unit tests in corresponding test project
3. Update configuration schema if needed
4. Always test with sample data after changes

### Debugging Failed Tests
1. Failed tests are often due to missing configuration files - this is expected
2. Focus on core functionality tests that should pass
3. Use verbose logging: `dotnet test -v detailed`

### Working with Azure Data Factory Components
- Located in `*/AzureDataFactoryPipeline/` directories
- PowerShell scripts for deployment in `*/scripts/` directories
- Contains ARM templates for Azure resource deployment
- Tests are often skipped (expected behavior)

### Modifying Configuration Files
- FHIR: Default config is `configuration-sample.json`
- DICOM: Default config is `configuration.json`
- Configurations define anonymization rules and methods
- Always validate configuration changes with actual data

## Important Notes

### Critical Timing Warnings
- **NEVER CANCEL BUILDS OR TESTS** - They may appear to hang but are processing
- FHIR build: ~3 seconds (set 120+ second timeout)
- DICOM build: ~2 seconds (set 120+ second timeout)
- FHIR tests: ~32 seconds (set 300+ second timeout)
- DICOM tests: ~10 seconds (set 180+ second timeout)

### Expected Behaviors
- StyleCop warnings are common and expected
- Some unit/functional tests fail due to missing files (expected)
- Code coverage only works on Windows (expected on Linux)
- Azure Data Factory tests are skipped (expected)

### Development Best Practices
- Always build in Release configuration for consistency with CI
- Test changes with actual sample data, not just unit tests
- Use the existing sample files for validation
- Focus changes on core anonymization functionality
- Maintain backward compatibility with existing configurations

### Troubleshooting
- If builds fail, ensure .NET 8.0 SDK is installed
- If tests fail unexpectedly, check for missing test data files
- If command-line tools fail, verify the configuration file exists
- For file path issues, use absolute paths when possible