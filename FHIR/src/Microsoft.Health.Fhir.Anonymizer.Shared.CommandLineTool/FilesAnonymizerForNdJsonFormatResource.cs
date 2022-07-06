using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;

namespace Microsoft.Health.Fhir.Anonymizer.Tool
{
    public class FilesAnonymizerForNdJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private string _configFilePath;
        private AnonymizationToolOptions _options;

        public FilesAnonymizerForNdJsonFormatResource(
            string configFilePath,
            string inputFolder,
            string outputFolder,
            AnonymizationToolOptions options)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _configFilePath = configFilePath;

            _options = options;
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
        }

        public async Task AnonymizeAsync()
        {
            var directorySearchOption = _options.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var bulkResourceFileList = Directory.EnumerateFiles(_inputFolder, "*.ndjson", directorySearchOption).ToList();
            Console.WriteLine($"Find {bulkResourceFileList.Count()} bulk data resource files in '{_inputFolder}'.");

            foreach (var bulkResourceFileName in bulkResourceFileList)
            {
                Console.WriteLine($"Processing {bulkResourceFileName}");

                var bulkResourceOutputFileName = GetResourceOutputFileName(bulkResourceFileName, _inputFolder, _outputFolder);
                var tempBulkResourceOutputFileName = GetTempFileName(bulkResourceOutputFileName);
                if (_options.IsRecursive)
                {
                    var resourceOutputFolder = Path.GetDirectoryName(bulkResourceOutputFileName);
                    Directory.CreateDirectory(resourceOutputFolder);
                }

                if (_options.SkipExistedFile && File.Exists(bulkResourceOutputFileName))
                {
                    Console.WriteLine($"Skip processing on file {bulkResourceOutputFileName} since it already exists in destination.");
                    continue;
                }
                else
                {
                    if (File.Exists(bulkResourceOutputFileName))
                    {
                        Console.WriteLine($"Remove existed target file {bulkResourceOutputFileName}.");
                        File.Delete(bulkResourceOutputFileName);
                    }
                }

                int completedCount = 0;
                int skippedCount = 0;
                int consumeCompletedCount = 0;
                using (FileStream inputStream = new FileStream(bulkResourceFileName, FileMode.Open))
                using (FileStream outputStream = new FileStream(tempBulkResourceOutputFileName, FileMode.Create))
                {
                    using FhirStreamReader reader = new FhirStreamReader(inputStream);
                    using FhirStreamConsumer consumer = new FhirStreamConsumer(outputStream);
                    var engine = AnonymizerEngine.CreateWithFileContext(_configFilePath, bulkResourceFileName, _inputFolder);
                    Func<string, string> anonymizeFunction = (content) =>
                    {
                        try
                        {
                            var settings = new AnonymizerSettings()
                            {
                                IsPrettyOutput = false,
                                ValidateInput = _options.ValidateInput,
                                ValidateOutput = _options.ValidateOutput
                            };
                            return engine.AnonymizeJson(content, settings);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"ErrorMessage: {ex}");
                            throw;
                        }
                    };

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, consumer, anonymizeFunction);
                    executor.PartitionCount = Environment.ProcessorCount * 2;

                    Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
                    progress.ProgressChanged += (obj, args) =>
                    {
                        Interlocked.Add(ref completedCount, args.ProcessCompleted);
                        Interlocked.Add(ref skippedCount, args.ProcessSkipped);
                        Interlocked.Add(ref consumeCompletedCount, args.ConsumeCompleted);

                        Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {skippedCount} Process skipped. {consumeCompletedCount} Consume completed.");
                    };

                    await executor.ExecuteAsync(CancellationToken.None, progress).ConfigureAwait(false);
                }

                // Rename file name after success process
                File.Move(tempBulkResourceOutputFileName, bulkResourceOutputFileName);

                Console.WriteLine($"Finished processing '{bulkResourceFileName}'!");
            }
        }

        private string GetTempFileName(string pathFileName)
        {
            string directory = Path.GetDirectoryName(pathFileName);

            return Path.Combine(directory, $"{Guid.NewGuid():N}");
        }

        private string GetResourceOutputFileName(string fileName, string inputFolder, string outputFolder)
        {
            var partialFilename = fileName.Substring(inputFolder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(outputFolder, partialFilename);
        }
    }
}
