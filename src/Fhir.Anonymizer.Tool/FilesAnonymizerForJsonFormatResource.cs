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
using Hl7.FhirPath.Sprache;

namespace Fhir.Anonymizer.Tool
{
    public class FilesAnonymizerForJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private bool _isRecursive;
        private AnonymizerEngine _engine;

        public FilesAnonymizerForJsonFormatResource(AnonymizerEngine engine, string inputFolder, string outputFolder, bool isRecursive)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _isRecursive = isRecursive;
            _engine = engine;
        }

        public async Task Anonymize()
        {
            var directorySearchOption = _isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var resourceFileList = Directory.EnumerateFiles(_inputFolder, "*.json", directorySearchOption).ToList();
            Console.WriteLine($"Find {resourceFileList.Count()} json resource files in '{_inputFolder}'.");

            FhirEnumerableReader<string> reader = new FhirEnumerableReader<string>(resourceFileList);
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, null, FileAnonymize)
            {
                KeepOrder = false,
                BatchSize = 1,
                PartitionCount = Environment.ProcessorCount
            };

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            int completedCount = 0;
            int failedCount = 0;
            Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
            progress.ProgressChanged += (obj, args) =>
            {
                Interlocked.Add(ref completedCount, args.ProcessCompleted);
                Interlocked.Add(ref failedCount, args.ProcessFailed);

                Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {failedCount} Process failed.");
            };

            await executor.ExecuteAsync(cancellationToken: CancellationToken.None, false, progress).ConfigureAwait(false);
        }

        public string FileAnonymize(string fileName)
        {
            var resourceOutputFileName = GetResourceOutputFileName(fileName, _inputFolder, _outputFolder);
            if (_isRecursive)
            {
                var resourceOutputFolder = Path.GetDirectoryName(resourceOutputFileName);
                Directory.CreateDirectory(resourceOutputFolder);
            }

            using (FileStream inputStream = new FileStream(fileName, FileMode.Open))
            using (FileStream outputStream = new FileStream(resourceOutputFileName, FileMode.Create))
            {
                using StreamReader reader = new StreamReader(inputStream);
                using StreamWriter writer = new StreamWriter(outputStream);
                var resourceJson = reader.ReadToEnd();
                try
                {
                    var resourceResult = _engine.AnonymizeJson(resourceJson, isPrettyOutput: true);
                    writer.Write(resourceResult);
                    writer.Flush();
                }
                catch (Exception innerException)
                {
                    Console.Error.WriteLine($"[{fileName}] Error:\nResource: {resourceJson}\nErrorMessage: {innerException.ToString()}");
                    throw;
                }
            }

            return string.Empty;
        }

        private string GetResourceOutputFileName(string fileName, string inputFolder, string outputFolder)
        {
            var partialFilename = fileName.Substring(inputFolder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(outputFolder, partialFilename);
        }
    }
}
