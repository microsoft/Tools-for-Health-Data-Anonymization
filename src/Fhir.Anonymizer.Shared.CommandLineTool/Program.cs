using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Tool    
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
        [Option('s', "skip", Required = false, Default = false, HelpText = "Skip existed files in target folder.")]
        public bool SkipExistedFile { get; set; }
        [Option('r', "recursive", Required = false, Default = false, HelpText = "Process resource files in input folder recursively.")]
        public bool IsRecursive { get; set; }
        [Option('v', "verbose", Required = false, Default = false, HelpText = "Provide additional details in processing.")]
        public bool IsVerbose { get; set; }
        [Option("validateInput", Required = false, Default = false, HelpText = "Validate input resources. Details can be found in verbose log.")]
        public bool ValidateInput { get; set; }
        [Option("validateOutput", Required = false, Default = false, HelpText = "Validate anonymized resources. Details can be found in verbose log.")]
        public bool ValidateOutput { get; set; }
    }

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await CommandLine.Parser.Default.ParseArguments<Options>(args)
               .MapResult(async options => await AnonymizationLogic.AnonymizeAsync(options).ConfigureAwait(false), _ => Task.FromResult(1)).ConfigureAwait(false);
        }        
    }
}
