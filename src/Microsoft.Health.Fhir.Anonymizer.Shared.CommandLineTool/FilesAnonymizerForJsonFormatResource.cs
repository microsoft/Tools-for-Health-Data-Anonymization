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
    public class FilesAnonymizerForJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private string _configFilePath;
        private AnonymizationToolOptions _options;

        public FilesAnonymizerForJsonFormatResource(
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
            var resourceFileList = Directory.EnumerateFiles(_inputFolder, "*.json", directorySearchOption).ToList();
            Console.WriteLine($"Find {resourceFileList.Count()} json resource files in '{_inputFolder}'.");

            FhirEnumerableReader<string> reader = new FhirEnumerableReader<string>(resourceFileList);
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, null)
            {
                KeepOrder = false,
                BatchSize = 1,
                PartitionCount = Environment.ProcessorCount * 2
            };

            executor.AnonymizerFunctionAsync = async file =>
            {
                try
                {
                    return await FileAnonymize(file).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error:\nResource: TOO_BIG\nErrorMessage: {ex.ToString()}");
                    throw;
                }
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

        public async Task<string> FileAnonymize(string fileName)
        {
            var resourceOutputFileName = GetResourceOutputFileName(fileName, _inputFolder, _outputFolder);
            if (_options.IsRecursive)
            {
                var resourceOutputFolder = Path.GetDirectoryName(resourceOutputFileName);
                Directory.CreateDirectory(resourceOutputFolder);
            }

            if (_options.SkipExistedFile && File.Exists(resourceOutputFileName))
            {
                Console.WriteLine($"Skip processing on file {fileName}.");
                return string.Empty;
            }

            string resourceJson = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
            try
            {
                var engine = AnonymizerEngine.CreateWithFileContext(_configFilePath, fileName, _inputFolder);
                var settings = new AnonymizerSettings()
                {
                    IsPrettyOutput = true,
                    ValidateInput = _options.ValidateInput,
                    ValidateOutput = _options.ValidateOutput
                };
                var resourceResult = engine.AnonymizeJson(resourceJson, settings);

                await File.WriteAllTextAsync(resourceOutputFileName, resourceResult).ConfigureAwait(false);
            }
            catch (Exception innerException)
            {
                Console.Error.WriteLine($"[{fileName}] Error:\nResource: TOO_BIG\nErrorMessage: {innerException.ToString()}");
                throw;
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
