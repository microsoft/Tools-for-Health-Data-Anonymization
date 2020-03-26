using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.PartitionedExecution;

namespace Fhir.Anonymizer.Tool
{
    public class FilesAnonymizerForNdJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private bool _isRecursive;
        private AnonymizerEngine _engine;

        public FilesAnonymizerForNdJsonFormatResource(AnonymizerEngine engine, string inputFolder, string outputFolder, bool isRecursive)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _isRecursive = isRecursive;
            _engine = engine;
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
                if (_isRecursive)
                {
                    var resourceOutputFolder = Path.GetDirectoryName(bulkResourceOutputFileName);
                    Directory.CreateDirectory(resourceOutputFolder);
                }

                int completedCount = 0;
                int failedCount = 0;
                int consumeCompletedCount = 0;
                using (FileStream inputStream = new FileStream(bulkResourceFileName, FileMode.Open))
                using (FileStream outputStream = new FileStream(bulkResourceOutputFileName, FileMode.Create))
                {
                    using FhirStreamReader reader = new FhirStreamReader(inputStream);
                    using FhirStreamConsumer consumer = new FhirStreamConsumer(outputStream);
                    Func<string, string> anonymizeFunction = (content) =>
                    {
                        try
                        {
                            return _engine.AnonymizeJson(content);
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

                    await executor.ExecuteAsync(CancellationToken.None, false, progress).ConfigureAwait(false);
                }

                Console.WriteLine($"Finished processing '{bulkResourceFileName}'!");
            }
        }

        private string GetResourceOutputFileName(string fileName, string inputFolder, string outputFolder)
        {
            var partialFilename = fileName.Substring(inputFolder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(outputFolder, partialFilename);
        }
    }
}
