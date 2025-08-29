using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core;

namespace Microsoft.Health.Fhir.Anonymizer.Tool    
{
    public class Program
    {
        public async static Task<int> Main(string[] args)
        {
            var inputOption = new Option<string>(
                new[] { "-i", "--inputFolder" },
                "Folder to locate input resource files.")
            {
                IsRequired = true
            };

            var outputOption = new Option<string>(
                new[] { "-o", "--outputFolder" },
                "Folder to save anonymized resource files.")
            {
                IsRequired = true
            };

            var configOption = new Option<string>(
                new[] { "-c", "--configFile" },
                () => "configuration-sample.json",
                "Anonymizer configuration file path.");

            var bulkDataOption = new Option<bool>(
                new[] { "-b", "--bulkData" },
                "Resource file is in bulk data format (.ndjson).");

            var skipOption = new Option<bool>(
                new[] { "-s", "--skip" },
                "Skip existed files in target folder.");

            var recursiveOption = new Option<bool>(
                new[] { "-r", "--recursive" },
                "Process resource files in input folder recursively.");

            var verboseOption = new Option<bool>(
                new[] { "-v", "--verbose" },
                "Provide additional details in processing.");

            var validateInputOption = new Option<bool>(
                "--validateInput",
                "Validate input resources. Details can be found in verbose log.");

            var validateOutputOption = new Option<bool>(
                "--validateOutput",
                "Validate anonymized resources. Details can be found in verbose log.");

            var rootCommand = new RootCommand("FHIR Data Anonymization Tool");
            rootCommand.AddOption(inputOption);
            rootCommand.AddOption(outputOption);
            rootCommand.AddOption(configOption);
            rootCommand.AddOption(bulkDataOption);
            rootCommand.AddOption(skipOption);
            rootCommand.AddOption(recursiveOption);
            rootCommand.AddOption(verboseOption);
            rootCommand.AddOption(validateInputOption);
            rootCommand.AddOption(validateOutputOption);

            rootCommand.SetHandler(async (context) =>
            {
                var inputFolder = context.ParseResult.GetValueForOption(inputOption);
                var outputFolder = context.ParseResult.GetValueForOption(outputOption);
                var configFile = context.ParseResult.GetValueForOption(configOption);
                var bulkData = context.ParseResult.GetValueForOption(bulkDataOption);
                var skip = context.ParseResult.GetValueForOption(skipOption);
                var recursive = context.ParseResult.GetValueForOption(recursiveOption);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);
                var validateInput = context.ParseResult.GetValueForOption(validateInputOption);
                var validateOutput = context.ParseResult.GetValueForOption(validateOutputOption);

                var options = new Options
                {
                    InputFolder = inputFolder,
                    OutputFolder = outputFolder,
                    ConfigurationFilePath = configFile,
                    IsBulkData = bulkData,
                    SkipExistedFile = skip,
                    IsRecursive = recursive,
                    IsVerbose = verbose,
                    ValidateInput = validateInput,
                    ValidateOutput = validateOutput
                };
                
                await AnonymizationLogic.AnonymizeAsync(options).ConfigureAwait(false);
            });

            try
            {
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }        
    }

    // Keep the existing Options class for internal use
    internal class Options
    {
        public string InputFolder { get; set; }
        public string OutputFolder { get; set; }
        public string ConfigurationFilePath { get; set; }
        public bool IsBulkData { get; set; }
        public bool SkipExistedFile { get; set; }
        public bool IsRecursive { get; set; }
        public bool IsVerbose { get; set; }
        public bool ValidateInput { get; set; }
        public bool ValidateOutput { get; set; }
    }
}
