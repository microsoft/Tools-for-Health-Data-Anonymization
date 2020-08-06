using System;
using System.IO;
using System.Threading.Tasks;
using MicrosoftFhir.Anonymizer.Core;
using Microsoft.Extensions.Logging;

namespace MicrosoftFhir.Anonymizer.Tool
{
    internal static class AnonymizationLogic
    {
        internal static async Task AnonymizeAsync(Options options)
        {
            try
            {
                InitializeAnonymizerLogging(options.IsVerbose);

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
                    await new FilesAnonymizerForNdJsonFormatResource(configFilePath, inputFolder, outputFolder, toolOptions).AnonymizeAsync().ConfigureAwait(false);
                }
                else
                {
                    await new FilesAnonymizerForJsonFormatResource(configFilePath, inputFolder, outputFolder, toolOptions).AnonymizeAsync().ConfigureAwait(false);
                }
                Console.WriteLine($"Finished processing '{inputFolder}'!");
            }
            finally
            {
                DisposeAnonymizerLogging();
            }
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
            AnonymizerLogging.LoggerFactory = LoggerFactory.Create(builder => {
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddFilter("Fhir.Anonymizer", isVerboseMode ? LogLevel.Trace : LogLevel.Information)
                       .AddConsole();
            });
        }

        private static void DisposeAnonymizerLogging()
        {
            AnonymizerLogging.LoggerFactory.Dispose();
        }
    }
}
