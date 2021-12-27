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
    public class FilesAnonymizerForJsonFormatResource
    {
        private string _inputFolder;
        private string _outputFolder;
        private string _configFilePath;
        private AnonymizationToolOptions _options;
        private bool _isR3;

        public FilesAnonymizerForJsonFormatResource(
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
            var resourceFileList = Directory.EnumerateFiles(_inputFolder, "*.json", directorySearchOption).ToList();
            Console.WriteLine($"Find {resourceFileList.Count()} json resource files in '{_inputFolder}'.");

            if (_isR3)
            {
                R3PartitionedExecution.FhirEnumerableReader<string> reader = new R3PartitionedExecution.FhirEnumerableReader<string>(resourceFileList);
                var executor = new R3PartitionedExecution.FhirPartitionedExecutor<string, string>(reader, null)
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
                        Console.Error.WriteLine($"Error:\nResource: {file}\nErrorMessage: {ex.ToString()}");
                        throw;
                    }
                };
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                int completedCount = 0;
                int skippedCount = 0;
                Progress<R3PartitionedExecution.BatchAnonymizeProgressDetail> progress = new Progress<R3PartitionedExecution.BatchAnonymizeProgressDetail>();
                progress.ProgressChanged += (obj, args) =>
                {
                    Interlocked.Add(ref completedCount, args.ProcessCompleted);
                    Interlocked.Add(ref skippedCount, args.ProcessSkipped);

                    Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {skippedCount} Process skipped.");
                };

                await executor.ExecuteAsync(cancellationToken: CancellationToken.None, progress).ConfigureAwait(false);
            }
            else
            {
                R4PartitionedExecution.FhirEnumerableReader<string> reader = new R4PartitionedExecution.FhirEnumerableReader<string>(resourceFileList);
                var executor = new R4PartitionedExecution.FhirPartitionedExecutor<string, string>(reader, null)
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
                        Console.Error.WriteLine($"Error:\nResource: {file}\nErrorMessage: {ex.ToString()}");
                        throw;
                    }
                };
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                int completedCount = 0;
                int skippedCount = 0;
                Progress<R4PartitionedExecution.BatchAnonymizeProgressDetail> progress = new Progress<R4PartitionedExecution.BatchAnonymizeProgressDetail>();
                progress.ProgressChanged += (obj, args) =>
                {
                    Interlocked.Add(ref completedCount, args.ProcessCompleted);
                    Interlocked.Add(ref skippedCount, args.ProcessSkipped);

                    Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {completedCount} Process completed. {skippedCount} Process skipped.");
                };

                await executor.ExecuteAsync(cancellationToken: CancellationToken.None, progress).ConfigureAwait(false);

            }
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
                Console.WriteLine($"Skip processing on file {fileName} since it already exists in destination.");
                return string.Empty;
            }

            string resourceJson = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
            try
            {
                if (_isR3)
                {
                    var engine = R3FhirCore.AnonymizerEngine.CreateWithFileContext(_configFilePath, fileName, _inputFolder);
                    var settings = new R3AnonymizerConfigurations.AnonymizerSettings()
                    {
                        IsPrettyOutput = true,
                        ValidateInput = _options.ValidateInput,
                        ValidateOutput = _options.ValidateOutput
                    };
                    var resourceResult = engine.AnonymizeJson(resourceJson, settings);
                    await File.WriteAllTextAsync(resourceOutputFileName, resourceResult).ConfigureAwait(false);
                    return resourceResult;
                }
                else
                {
                    var engine = R4FhirCore.AnonymizerEngine.CreateWithFileContext(_configFilePath, fileName, _inputFolder);
                    var settings = new R4AnonymizerConfigurations.AnonymizerSettings()
                    {
                        IsPrettyOutput = true,
                        ValidateInput = _options.ValidateInput,
                        ValidateOutput = _options.ValidateOutput
                    };
                    var resourceResult = engine.AnonymizeJson(resourceJson, settings);
                    await File.WriteAllTextAsync(resourceOutputFileName, resourceResult).ConfigureAwait(false);
                    return resourceResult;
                }
            }
            catch (Exception innerException)
            {
                Console.Error.WriteLine($"[{fileName}] Error:\nResource: {resourceJson}\nErrorMessage: {innerException.ToString()}");
                throw;
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
