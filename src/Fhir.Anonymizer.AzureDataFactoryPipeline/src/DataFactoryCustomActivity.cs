using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Fhir.Anonymizer.AzureDataFactoryPipeline.src;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.PartitionedExecution;
using Newtonsoft.Json;

namespace Fhir.Anonymizer.DataFactoryTool
{
    public class DataFactoryCustomActivity
    {
        private AnonymizerEngine _engine;
        private readonly string _activityConfigurationFile = "activity.json";
        private readonly string _datasetsConfigurationFile = "datasets.json";

        public ActivityInputData LoadActivityInput()
        {
            dynamic datasets = JsonConvert.DeserializeObject(File.ReadAllText(_datasetsConfigurationFile));
            var sourceContainerName = (string)datasets[0].properties.typeProperties.location.container;
            var sourceFolderPath = (string)datasets[0].properties.typeProperties.location.folderPath;
            var destinationContainerName = (string)datasets[1].properties.typeProperties.location.container;
            var destinationFolderPath = (string)datasets[1].properties.typeProperties.location.folderPath;

            dynamic activity = JsonConvert.DeserializeObject(File.ReadAllText(_activityConfigurationFile));
            var sourceConnectionString = (string)activity.typeProperties.extendedProperties.sourceConnectionString;
            var destinationConnectionString = (string)activity.typeProperties.extendedProperties.destinationConnectionString;

            return new ActivityInputData
            {
                SourceContainerName = sourceContainerName,
                SourceFolderPath = sourceFolderPath,
                DestinationContainerName = destinationContainerName,
                DestinationFolderPath = destinationFolderPath,
                SourceStorageConnectionString = sourceConnectionString,
                DestinationStorageConnectionString = destinationConnectionString
            };
        }

        public async Task AnonymizeDataset(ActivityInputData inputData, bool force)
        {
            string inputContainerName = inputData.SourceContainerName.ToLower();
            var inputContainer = new BlobContainerClient(inputData.SourceStorageConnectionString, inputContainerName);
            if (!await inputContainer.ExistsAsync())
            {
                throw new Exception($"Error: The specified container {inputData.SourceContainerName} does not exist.");
            }

            string outputContainerName = inputData.DestinationContainerName.ToLower();
            var outputContainer = new BlobContainerClient(inputData.DestinationStorageConnectionString, outputContainerName);
            await outputContainer.CreateIfNotExistsAsync();

            string inputBlobPrefix = GetBlobPrefixFromFolderPath(inputData.SourceFolderPath); ;
            string outputBlobPrefix = GetBlobPrefixFromFolderPath(inputData.DestinationFolderPath); ;

            var skippedBlobCount = 0;
            var skippedBlobList = new List<string>();

            await foreach (BlobItem blob in inputContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, inputBlobPrefix, default))
            {
                string outputBlobName = GetOutputBlobName(blob.Name, inputBlobPrefix, outputBlobPrefix);
                Console.WriteLine($"[{blob.Name}]：Processing... output to container '{outputContainerName}'");

                var inputBlobClient = new BlobClient(inputData.SourceStorageConnectionString, inputContainerName, blob.Name, GetBlobClientOptions());
                var outputBlobClient = new BlockBlobClient(inputData.DestinationStorageConnectionString, outputContainerName, outputBlobName, GetBlobClientOptions());

                var isOutputExist = await outputBlobClient.ExistsAsync();
                if (!force && isOutputExist)
                {
                    Console.WriteLine($"Blob file {blob.Name} already exists in {inputData.DestinationContainerName}, skipping..");
                    skippedBlobCount += 1;
                    skippedBlobList.Add(blob.Name);
                    continue;
                }
                else if (force && isOutputExist)
                {
                    await outputBlobClient.DeleteAsync();
                }

                if (IsInputFileInJsonFormat(blob.Name))
                {
                    await AnonymizeBlobInJsonFormatAsync(inputBlobClient, outputBlobClient, blob.Name);
                }
                else
                {
                    await AnonymizeBlobInNdJsonFormatAsync(inputBlobClient, outputBlobClient, blob.Name);
                }
            }

            if (skippedBlobCount > 0)
            {
                Console.WriteLine($"Skipped {skippedBlobCount} files already exists in destination container: {skippedBlobList.ToString()}");
                Console.WriteLine($"If you want to overwrite existing blob in {inputData.DestinationContainerName} container, please use the -f or --force flag");
            }
        }

        private async Task AnonymizeBlobInJsonFormatAsync(BlobClient inputBlobClient, BlockBlobClient outputBlobClient, string blobName)
        {
            try
            {
                var inputDownloadInfo = await inputBlobClient.DownloadAsync();
                using (var reader = new StreamReader(inputDownloadInfo.Value.Content))
                {
                    string input = await reader.ReadToEndAsync();
                    string output = _engine.AnonymizeJson(input, isPrettyOutput: true);

                    using (MemoryStream outputStream = new MemoryStream(reader.CurrentEncoding.GetBytes(output)))
                    {
                        await outputBlobClient.UploadAsync(outputStream);
                    }

                    Console.WriteLine($"[{blobName}]: Anonymize completed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{blobName}]: Anonymize failed, you can find detail error message in stderr.txt.");
                Console.Error.WriteLine($"[{blobName}]: Failed to anonymize blob. \nErrorMessage: {ex.Message}\n Details: {ex.ToString()} \nStackTrace: {ex.StackTrace}");
            }
        }

        private async Task AnonymizeBlobInNdJsonFormatAsync(BlobClient inputBlobClient, BlockBlobClient outputBlobClient, string blobName)
        {
            var processedCount = 0;
            var processedErrorCount = 0;
            var consumedCount = 0;

            using FhirBlobDataStream inputStream = new FhirBlobDataStream(inputBlobClient);
            FhirStreamReader reader = new FhirStreamReader(inputStream);
            FhirBlobConsumer consumer = new FhirBlobConsumer(outputBlobClient);
            Func<string, string> anonymizerFunction = (item) =>
            {
                try
                {
                    return _engine.AnonymizeJson(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{blobName}]: Anonymize partial failed, you can find detail error message in stderr.txt.");
                    Console.Error.WriteLine($"[{blobName}]: Resource: {item}\nErrorMessage: {ex.Message}\n Details: {ex.ToString()}\nStackTrace: {ex.StackTrace}");
                    throw;
                }
            };

            Stopwatch stopWatch = Stopwatch.StartNew();
            FhirPartitionedExecutor executor = new FhirPartitionedExecutor(reader, consumer, anonymizerFunction);
            executor.PartitionCount = Environment.ProcessorCount;
            Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
            progress.ProgressChanged += (obj, args) =>
            {
                Interlocked.Add(ref processedCount, args.ProcessCompleted);
                Interlocked.Add(ref processedErrorCount, args.ProcessFailed);
                Interlocked.Add(ref consumedCount, args.ConsumeCompleted);

                Console.WriteLine($"[{stopWatch.Elapsed.ToString()}]: {processedCount} Completed. {processedErrorCount} Failed. {consumedCount} consume completed.");
            };

            await executor.ExecuteAsync(CancellationToken.None, false, progress).ConfigureAwait(false);
        }

        private string GetBlobPrefixFromFolderPath(string folderPath)
        {
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                return $"{folderPath.TrimEnd('/')}/";
            }

            return string.Empty;
        }

        private string GetOutputBlobName(string blobName, string inputPrefix, string outputPrefix)
        {
            return $"{outputPrefix}{blobName.Substring(inputPrefix.Length)}";
        }

        private bool IsInputFileInJsonFormat(string fileName)
        {
            return ".json".Equals(Path.GetExtension(fileName), StringComparison.InvariantCultureIgnoreCase);
        }

        private BlobClientOptions GetBlobClientOptions()
        {
            BlobClientOptions options = new BlobClientOptions();
            options.Retry.Delay = TimeSpan.FromSeconds(30);
            options.Retry.Mode = Azure.Core.RetryMode.Exponential;
            options.Retry.MaxDelay = TimeSpan.FromSeconds(100);
            options.Retry.MaxRetries = 3;

            return options;
        }

        public async Task Run(bool force = false)
        {
            _engine = new AnonymizerEngine("./configuration-sample.json");

            var input = LoadActivityInput();
            await AnonymizeDataset(input, force).ConfigureAwait(false);
        }
    }
}
