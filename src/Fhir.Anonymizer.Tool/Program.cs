using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Fhir.Anonymizer.Core;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Tool    
{
    public class Program
    {
        class Options
        {
            [Option('i', "inputFolder", Required = true, HelpText = "Folder to locate input resource files.")]
            public string InputFolder { get; set; }
            [Option('o', "outputFolder", Required = true, HelpText = "Folder to save anonymized resource files.")]
            public string OutputFolder { get; set; }
            [Option('c', "configFile", Required = false, Default = "configuration-sample.json", HelpText = "Anonymizer configuration file path.")]
            public string ConfigurationFilePath { get; set; }
            [Option('b', "bulkData", Required = false, Default = false, HelpText = "Resource file is in bulk data format (.ndjson).")]
            public bool IsBulkData { get; set; }
            [Option('r', "recursive", Required = false, Default = false, HelpText = "Process resource files in input folder recursively.")]
            public bool IsRecursive { get; set; }
            [Option('v', "verbose", Required = false, Default = false, HelpText = "Provide additional details in processing.")]
            public bool IsVerbose { get; set; }
        }

        public async static Task Main(string[] args)
        {
            await CommandLine.Parser.Default.ParseArguments<Options>(args)
               .MapResult(async options => await RunAnonymization(options).ConfigureAwait(false), _ => Task.FromResult(1)).ConfigureAwait(false);
        }

        private async static Task RunAnonymization(Options options)
        {
            InitializeAnonymizerLogging(options.IsVerbose);

            var dataProcessor = new FhirResourceDataProcessor(options.ConfigurationFilePath);
            if (dataProcessor.IsSameDirectory(options.InputFolder, options.OutputFolder))
            {
                throw new Exception("Input and output folders are the same! Please choose another folder.");
            }

            Directory.CreateDirectory(options.OutputFolder);

            if (options.IsBulkData)
            {
                await dataProcessor.AnonymizeBulkDataFolder(options.InputFolder, options.OutputFolder, options.IsRecursive).ConfigureAwait(false);
            }
            else
            {
                await dataProcessor.AnonymizeFolder(options.InputFolder, options.OutputFolder, options.IsRecursive).ConfigureAwait(false);
            }
        }

        private static void InitializeAnonymizerLogging(bool isVerboseMode)
        {
            AnonymizerLogging.LoggerFactory = LoggerFactory.Create(builder => {
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddFilter("Fhir.Anonymizer", isVerboseMode ? LogLevel.Trace : LogLevel.Information)
                       .AddConsole();
            });
        }
    }
}
