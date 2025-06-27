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

REM Define project configurations to eliminate duplication
set DICOM_SOLUTION=DICOM\Dicom.Anonymizer.sln
set DICOM_NAME=DICOM Anonymizer
set FHIR_SOLUTION=FHIR\Fhir.Anonymizer.sln
set FHIR_NAME=FHIR Anonymizer

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
call :build_project "%DICOM_SOLUTION%" "%DICOM_NAME%"
call :build_project "%FHIR_SOLUTION%" "%FHIR_NAME%"
goto end

:build_dicom
call :build_project "%DICOM_SOLUTION%" "%DICOM_NAME%"
goto :eof

:build_fhir
call :build_project "%FHIR_SOLUTION%" "%FHIR_NAME%"
goto :eof

:build_project
set SOLUTION_PATH=%~1
set PROJECT_NAME=%~2
echo Building %PROJECT_NAME%...
if "%FRAMEWORK%"=="all" (
    dotnet build "%SOLUTION_PATH%" -c %CONFIG% --maxcpucount:1
) else (
    dotnet build "%SOLUTION_PATH%" -c %CONFIG% -f %FRAMEWORK% --maxcpucount:1
)
if errorlevel 1 goto error
echo %PROJECT_NAME% build completed successfully.
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
