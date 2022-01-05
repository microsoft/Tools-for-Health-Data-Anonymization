extern alias R3Core;
extern alias R4Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using R3FhirCore = R3Core::Microsoft.Health.Fhir.Anonymizer.Core;
using R4FhirCore = R4Core::Microsoft.Health.Fhir.Anonymizer.Core;
using R3AnonymizerConfigurations = R3Core::Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using R4AnonymizerConfigurations = R4Core::Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;

namespace Microsoft.Health.Fhir.Anonymizer.Tool
{
    internal static class AnonymizationLogic
    {
        public static bool IsR3 { get; set; }
        private static Options _options;

        internal static async Task AnonymizeAsync(Options options)
        {
            _options = options;
            IsR3 = IsR3Type(options.ConfigurationFilePath);
            try
            {
                InitializeAnonymizerLogging(options.IsVerbose);
                if(options.InputFile!= null && options.OutputFile != null)
                {
                    await AnonymizeFileAsync(options.InputFile, options.OutputFile, options.ConfigurationFilePath);
                    Console.WriteLine($"Finished processing '{options.InputFile}'!");
                }
                else
                {
                    if (IsSameDirectory(options.InputFolder, options.OutputFolder))
                    {
                        throw new Exception("Input and output folders are the same! Please choose another folder.");
                    }

                    Directory.CreateDirectory(options.OutputFolder);

                    string inputFolder = options.InputFolder;
                    string outputFolder = options.OutputFolder;
                    string configFilePath = options.ConfigurationFilePath;

                    AnonymizationToolOptions toolOptions = new AnonymizationToolOptions()
                    {
                        IsRecursive = options.IsRecursive,
                        SkipExistedFile = options.SkipExistedFile,
                        ValidateInput = options.ValidateInput,
                        ValidateOutput = options.ValidateOutput
                    };


                    if (options.IsBulkData)
                    {
                        await new FilesAnonymizerForNdJsonFormatResource(configFilePath, inputFolder, outputFolder, toolOptions, IsR3).AnonymizeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await new FilesAnonymizerForJsonFormatResource(configFilePath, inputFolder, outputFolder, toolOptions, IsR3).AnonymizeAsync().ConfigureAwait(false);
                    }
                    Console.WriteLine($"Finished processing '{inputFolder}'!");
                }
            }
            finally
            {
                DisposeAnonymizerLogging();
            }
        }

        private static async Task AnonymizeFileAsync(string input, string output, string configFilePath)
        {
            if (IsR3)
            {
                R3FhirCore.AnonymizerEngine.InitializeFhirPathExtensionSymbols();
                var engine = new R3FhirCore.AnonymizerEngine(configFilePath);
                var settings = new R3AnonymizerConfigurations.AnonymizerSettings()
                {
                    IsPrettyOutput = true,
                    ValidateInput = _options.ValidateInput,
                    ValidateOutput = _options.ValidateOutput
                };
                var resourceResult = engine.AnonymizeJson(File.ReadAllText(input), settings);
                await File.WriteAllTextAsync(output, resourceResult).ConfigureAwait(false);
            }
            else
            {
                R4FhirCore.AnonymizerEngine.InitializeFhirPathExtensionSymbols();
                var engine = new R4FhirCore.AnonymizerEngine(configFilePath);
                var settings = new R4AnonymizerConfigurations.AnonymizerSettings()
                {
                    IsPrettyOutput = true,
                    ValidateInput = _options.ValidateInput,
                    ValidateOutput = _options.ValidateOutput
                };
                var resourceResult = engine.AnonymizeJson(File.ReadAllText(input), settings);
                await File.WriteAllTextAsync(output, resourceResult).ConfigureAwait(false);
            }
        }
        private static bool IsR3Type(string configFilePath)
        {
            var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(configFilePath));
            var type = content["fhirVersion"].ToString();
            return string.Equals(type, "Stu3", StringComparison.OrdinalIgnoreCase) ? true : false;
        }

        private static bool IsSameDirectory(string inputFolder, string outputFolder)
        {
            string inputFolderPath = Path.GetFullPath(inputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputFolderPath = Path.GetFullPath(outputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(inputFolderPath, outputFolderPath, StringComparison.InvariantCultureIgnoreCase);
        }

        private static void InitializeAnonymizerLogging(bool isVerboseMode)
        {
            if (IsR3)
            {
                R3FhirCore.AnonymizerLogging.LoggerFactory = LoggerFactory.Create(builder => {
                    builder.AddFilter("Microsoft", LogLevel.Warning)
                           .AddFilter("System", LogLevel.Warning)
                           .AddFilter("Fhir.Anonymizer", isVerboseMode ? LogLevel.Trace : LogLevel.Information);
                });
            }
            else
            {
                R4FhirCore.AnonymizerLogging.LoggerFactory = LoggerFactory.Create(builder => {
                    builder.AddFilter("Microsoft", LogLevel.Warning)
                           .AddFilter("System", LogLevel.Warning)
                           .AddFilter("Fhir.Anonymizer", isVerboseMode ? LogLevel.Trace : LogLevel.Information);
                });
            }
        }

        private static void DisposeAnonymizerLogging()
        {
            if (IsR3)
            {
                R3FhirCore.AnonymizerLogging.LoggerFactory.Dispose();
            }
            else
            {
                R4FhirCore.AnonymizerLogging.LoggerFactory.Dispose();
            }
        }
    }
    
}
