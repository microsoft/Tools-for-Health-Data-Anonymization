using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.PartitionedExecution;

namespace Fhir.Anonymizer.Tool
{
    public class FilesAnonymizerForNdJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private bool _isRecursive;
        private bool _validateInput;
        private bool _validateOutput;
        private bool _skipExistedFile;
        private string _configFilePath;

        public FilesAnonymizerForNdJsonFormatResource(
            string configFilePath,
            string inputFolder,
            string outputFolder,
            bool isRecursive,
            bool validateInput,
            bool validateOutput,
            bool skipExistedFile)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _isRecursive = isRecursive;
            _validateInput = validateInput;
            _validateOutput = validateOutput;
            _configFilePath = configFilePath;
            _skipExistedFile = skipExistedFile;
        }

        public async Task AnonymizeAsync()
        {
            var directorySearchOption = _isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var bulkResourceFileList = Directory.EnumerateFiles(_inputFolder, "*.ndjson", directorySearchOption).ToList();
            Console.WriteLine($"Find {bulkResourceFileList.Count()} bulk data resource files in '{_inputFolder}'.");

            foreach (var bulkResourceFileName in bulkResourceFileList)
            {
                Console.WriteLine($"Processing {bulkResourceFileName}");

                var bulkResourceOutputFileName = GetResourceOutputFileName(bulkResourceFileName, _inputFolder, _outputFolder);
                var tempBulkResourceOutputFileName = GetTempFileName(bulkResourceOutputFileName);
                if (_isRecursive)
                {
                    var resourceOutputFolder = Path.GetDirectoryName(bulkResourceOutputFileName);
                    Directory.CreateDirectory(resourceOutputFolder);
                }

                if (_skipExistedFile && File.Exists(bulkResourceOutputFileName))
                {
                    Console.WriteLine($"Skip processing on file {bulkResourceOutputFileName}.");
                    continue;
                }
                else
                {
                    if (File.Exists(bulkResourceOutputFileName))
                    {
                        Console.WriteLine($"Remove exsited target file {bulkResourceOutputFileName}.");
                        File.Delete(bulkResourceOutputFileName);
                    }
                }

                int completedCount = 0;
                int failedCount = 0;
                int consumeCompletedCount = 0;
                using (FileStream inputStream = new FileStream(bulkResourceFileName, FileMode.Open))
                using (FileStream outputStream = new FileStream(tempBulkResourceOutputFileName, FileMode.Create))
                {
                    using FhirStreamReader reader = new FhirStreamReader(inputStream);
                    using FhirStreamConsumer consumer = new FhirStreamConsumer(outputStream);
                    Func<string, string> anonymizeFunction = (content) =>
                    {
                        try
                        {
                            var engine = AnonymizerEngine.CreateWithFileContext(_configFilePath, bulkResourceFileName, _inputFolder);
                            var settings = new AnonymizerSettings()
                            {
                                IsPrettyOutput = false,
                                ValidateInput = _validateInput,
                                ValidateOutput = _validateOutput
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

                    FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, consumer, anonymizeFunction);
                    executor.PartitionCount = Environment.ProcessorCount * 2;

                    Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
                    progress.ProgressChanged += (obj, args) =>
                    {
                        Interlocked.Add(ref completedCount, args.ProcessCompleted);
                        Interlocked.Add(ref failedCount, args.ProcessFailed);
                        Interlocked.Add(ref consumeCompletedCount, args.ConsumeCompleted);

                        Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {failedCount} Process failed. {consumeCompletedCount} Consume completed.");
                    };

                    await executor.ExecuteAsync(CancellationToken.None, true, progress).ConfigureAwait(false);
                }

                // Rename file name after success process
                File.Move(tempBulkResourceOutputFileName, bulkResourceOutputFileName);

                Console.WriteLine($"Finished processing '{bulkResourceFileName}'!");
            }
        }

        private string GetTempFileName(string pathFileName)
        {
            string fileName = Path.GetFileName(pathFileName);
            string directory = Path.GetDirectoryName(pathFileName);

            return Path.Combine(directory, $"temp_{fileName}");
        }

        private string GetResourceOutputFileName(string fileName, string inputFolder, string outputFolder)
        {
            var partialFilename = fileName.Substring(inputFolder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(outputFolder, partialFilename);
        }
    }
}
