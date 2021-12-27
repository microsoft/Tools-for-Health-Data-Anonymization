extern alias R3Core;
extern alias R4Core;

using R3FhirCore = R3Core::Microsoft.Health.Fhir.Anonymizer.Core;
using R4FhirCore = R4Core::Microsoft.Health.Fhir.Anonymizer.Core;
using R3PartitionedExecution = R3Core::Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;
using R4PartitionedExecution = R4Core::Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;
using R3AnonymizerConfigurations = R3Core::Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using R4AnonymizerConfigurations = R4Core::Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.Health.Fhir.Anonymizer.Tool
{
    public class FilesAnonymizerForNdJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private string _configFilePath;
        private AnonymizationToolOptions _options;
        private bool _isR3;

        public FilesAnonymizerForNdJsonFormatResource(
            string configFilePath,
            string inputFolder,
            string outputFolder,
            AnonymizationToolOptions options, bool IsR3)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _configFilePath = configFilePath;

            _options = options;
            _isR3 = IsR3;
            if (IsR3)
            {
                R3FhirCore.AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            }
            else
            {
                R4FhirCore.AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            }
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
                if (_isR3)
                {
                    AnonymizeR3BulkAsync(bulkResourceFileName, tempBulkResourceOutputFileName);
                }
                else
                {
                    AnonymizeR4BulkAsync(bulkResourceFileName, tempBulkResourceOutputFileName);
                }



                // Rename file name after success process
                File.Move(tempBulkResourceOutputFileName, bulkResourceOutputFileName);

                Console.WriteLine($"Finished processing '{bulkResourceFileName}'!");
            }
        }

        public async Task AnonymizeR3BulkAsync(string bulkResourceFileName, string tempBulkResourceOutputFileName)
        {

            int completedCount = 0;
            int skippedCount = 0;
            int consumeCompletedCount = 0;
            using (FileStream inputStream = new FileStream(bulkResourceFileName, FileMode.Open))
            using (FileStream outputStream = new FileStream(tempBulkResourceOutputFileName, FileMode.Create))
            {
                using R3PartitionedExecution.FhirStreamReader reader = new R3PartitionedExecution.FhirStreamReader(inputStream);
                using R3PartitionedExecution.FhirStreamConsumer consumer = new R3PartitionedExecution.FhirStreamConsumer(outputStream);
                var engine = R3FhirCore.AnonymizerEngine.CreateWithFileContext(_configFilePath, bulkResourceFileName, _inputFolder);
                Func<string, string> anonymizeFunction = (content) =>
                {
                    try
                    {
                        var settings = new R3AnonymizerConfigurations.AnonymizerSettings()
                        {
                            IsPrettyOutput = false,
                            ValidateInput = _options.ValidateInput,
                            ValidateOutput = _options.ValidateOutput
                        };
                        return engine.AnonymizeJson(content, settings);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error:\nResource: {content}\nErrorMessage: {ex.ToString()}");
                        throw;
                    }
                };

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                R3PartitionedExecution.FhirPartitionedExecutor<string, string> executor = new R3PartitionedExecution.FhirPartitionedExecutor<string, string>(reader, consumer, anonymizeFunction);
                executor.PartitionCount = Environment.ProcessorCount * 2;

                Progress<R3PartitionedExecution.BatchAnonymizeProgressDetail> progress = new Progress<R3PartitionedExecution.BatchAnonymizeProgressDetail>();
                progress.ProgressChanged += (obj, args) =>
                {
                    Interlocked.Add(ref completedCount, args.ProcessCompleted);
                    Interlocked.Add(ref skippedCount, args.ProcessSkipped);
                    Interlocked.Add(ref consumeCompletedCount, args.ConsumeCompleted);

                    Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {skippedCount} Process skipped. {consumeCompletedCount} Consume completed.");
                };

                await executor.ExecuteAsync(CancellationToken.None, progress).ConfigureAwait(false);
            }

        }

        public async Task AnonymizeR4BulkAsync(string bulkResourceFileName, string tempBulkResourceOutputFileName)
        {

            int completedCount = 0;
            int skippedCount = 0;
            int consumeCompletedCount = 0;
            using (FileStream inputStream = new FileStream(bulkResourceFileName, FileMode.Open))
            using (FileStream outputStream = new FileStream(tempBulkResourceOutputFileName, FileMode.Create))
            {
                using R4PartitionedExecution.FhirStreamReader reader = new R4PartitionedExecution.FhirStreamReader(inputStream);
                using R4PartitionedExecution.FhirStreamConsumer consumer = new R4PartitionedExecution.FhirStreamConsumer(outputStream);
                var engine = R4FhirCore.AnonymizerEngine.CreateWithFileContext(_configFilePath, bulkResourceFileName, _inputFolder);
                Func<string, string> anonymizeFunction = (content) =>
                {
                    try
                    {
                        var settings = new R4AnonymizerConfigurations.AnonymizerSettings()
                        {
                            IsPrettyOutput = false,
                            ValidateInput = _options.ValidateInput,
                            ValidateOutput = _options.ValidateOutput
                        };
                        return engine.AnonymizeJson(content, settings);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error:\nResource: {content}\nErrorMessage: {ex.ToString()}");
                        throw;
                    }
                };

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                R4PartitionedExecution.FhirPartitionedExecutor<string, string> executor = new R4PartitionedExecution.FhirPartitionedExecutor<string, string>(reader, consumer, anonymizeFunction);
                executor.PartitionCount = Environment.ProcessorCount * 2;

                Progress<R4PartitionedExecution.BatchAnonymizeProgressDetail> progress = new Progress<R4PartitionedExecution.BatchAnonymizeProgressDetail>();
                progress.ProgressChanged += (obj, args) =>
                {
                    Interlocked.Add(ref completedCount, args.ProcessCompleted);
                    Interlocked.Add(ref skippedCount, args.ProcessSkipped);
                    Interlocked.Add(ref consumeCompletedCount, args.ConsumeCompleted);

                    Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {skippedCount} Process skipped. {consumeCompletedCount} Consume completed.");
                };

                await executor.ExecuteAsync(CancellationToken.None, progress).ConfigureAwait(false);
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
