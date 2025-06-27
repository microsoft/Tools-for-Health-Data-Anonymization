@echo off
REM Multi-Target Build Script for Health Data Anonymization Tools
REM Usage: build.bat [net6.0|net7.0|net8.0|all] [DICOM|FHIR|all] [Debug|Release]

setlocal

set FRAMEWORK=%1
set PROJECT=%2
set CONFIG=%3

if "%FRAMEWORK%"=="" set FRAMEWORK=all
if "%PROJECT%"=="" set PROJECT=all
if "%CONFIG%"=="" set CONFIG=Release

echo.
echo Health Data Anonymization Tools - Multi-Target Build
echo ===================================================
echo Framework: %FRAMEWORK%
echo Project: %PROJECT%
echo Configuration: %CONFIG%
echo.

if "%PROJECT%"=="all" goto build_all
if "%PROJECT%"=="DICOM" goto build_dicom
if "%PROJECT%"=="FHIR" goto build_fhir
goto build_all

:build_all
call :build_dicom
call :build_fhir
goto end

:build_dicom
echo Building DICOM Anonymizer...
if "%FRAMEWORK%"=="all" (
    dotnet build "DICOM\Dicom.Anonymizer.sln" -c %CONFIG% --maxcpucount:1
) else (
    dotnet build "DICOM\Dicom.Anonymizer.sln" -c %CONFIG% -f %FRAMEWORK% --maxcpucount:1
)
if errorlevel 1 goto error
echo DICOM build completed successfully.
echo.
goto :eof

:build_fhir
echo Building FHIR Anonymizer...
if "%FRAMEWORK%"=="all" (
    dotnet build "FHIR\Fhir.Anonymizer.sln" -c %CONFIG% --maxcpucount:1
) else (
    dotnet build "FHIR\Fhir.Anonymizer.sln" -c %CONFIG% -f %FRAMEWORK% --maxcpucount:1
)
if errorlevel 1 goto error
echo FHIR build completed successfully.
echo.
goto :eof

:error
echo.
echo BUILD FAILED!
exit /b 1

:end
echo.
echo All builds completed successfully!
echo.
echo Usage Examples:
echo   build.bat net6.0 DICOM Release
echo   build.bat net8.0 FHIR Debug
echo   build.bat all all Release
echo.
