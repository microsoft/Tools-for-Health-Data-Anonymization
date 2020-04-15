using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Fhir.Anonymizer.AzureDataFactoryPipeline.src;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.PartitionedExecution;
using Newtonsoft.Json;

namespace Fhir.Anonymizer.DataFactoryTool
{
    public class DataFactoryCustomActivity
    {
        private readonly string _activityConfigurationFile = "activity.json";
        private readonly string _datasetsConfigurationFile = "datasets.json";
        private readonly string _configFile = "./configuration-sample.json";

        public static Lazy<BlobClientOptions> BlobClientOptions = new Lazy<BlobClientOptions>( () =>
        {
            BlobClientOptions options = new BlobClientOptions();
            options.Retry.Delay = TimeSpan.FromSeconds(FhirAzureConstants.StorageOperationRetryDelayInSeconds);
            options.Retry.Mode = Azure.Core.RetryMode.Exponential;
            options.Retry.MaxDelay = TimeSpan.FromSeconds(FhirAzureConstants.StorageOperationRetryMaxDelayInSeconds);
            options.Retry.MaxRetries = FhirAzureConstants.StorageOperationRetryCount;

            return options;
        });

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

            await AnonymizeBlobsInJsonFormat(inputData, inputContainer, outputContainer, inputBlobPrefix, outputBlobPrefix).ConfigureAwait(false);
            await AnonymizeBlobsInNdJsonFormat(inputData, inputContainer, outputContainer, inputBlobPrefix, outputBlobPrefix).ConfigureAwait(false);
        }

        private async Task AnonymizeBlobsInJsonFormat(ActivityInputData inputData, BlobContainerClient inputContainer, BlobContainerClient outputContainer, string inputBlobPrefix, string outputBlobPrefix)
        {
            IEnumerable<BlobItem> blobsInJsonFormat = inputContainer.GetBlobs(BlobTraits.None, BlobStates.None, inputBlobPrefix, default).Where(blob => IsInputFileInJsonFormat(blob.Name));
            FhirEnumerableReader<BlobItem> reader = new FhirEnumerableReader<BlobItem>(blobsInJsonFormat);
            Func<BlobItem, Task<string>> anonymizeBlobFunc = async (blob) =>
            {
                string outputBlobName = GetOutputBlobName(blob.Name, inputBlobPrefix, outputBlobPrefix);
                Console.WriteLine($"[{blob.Name}]：Processing... output to container '{outputContainer.Name}'");

                var inputBlobClient = new BlobClient(inputData.SourceStorageConnectionString, inputContainer.Name, blob.Name, BlobClientOptions.Value);
                var outputBlobClient = new BlockBlobClient(inputData.DestinationStorageConnectionString, outputContainer.Name, outputBlobName, BlobClientOptions.Value);
                await outputBlobClient.DeleteIfExistsAsync().ConfigureAwait(false);

                await AnonymizeSingleBlobInJsonFormatAsync(inputBlobClient, outputBlobClient, blob.Name, inputBlobPrefix).ConfigureAwait(false);

                return string.Empty;
            };

            FhirPartitionedExecutor<BlobItem, string> executor = new FhirPartitionedExecutor<BlobItem, string>(reader, null, anonymizeBlobFunc);
            executor.PartitionCount = Environment.ProcessorCount * 2;
            executor.BatchSize = 1;

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task AnonymizeBlobsInNdJsonFormat(ActivityInputData inputData, BlobContainerClient inputContainer, BlobContainerClient outputContainer, string inputBlobPrefix, string outputBlobPrefix)
        {
            await foreach (BlobItem blob in inputContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, inputBlobPrefix, default))
            {
                if (IsInputFileInJsonFormat(blob.Name))
                {
                    continue;
                }
                
                string outputBlobName = GetOutputBlobName(blob.Name, inputBlobPrefix, outputBlobPrefix);
                Console.WriteLine($"[{blob.Name}]：Processing... output to container '{outputContainer.Name}'");

                var inputBlobClient = new BlobClient(inputData.SourceStorageConnectionString, inputContainer.Name, blob.Name, BlobClientOptions.Value);
                var outputBlobClient = new BlockBlobClient(inputData.DestinationStorageConnectionString, outputContainer.Name, outputBlobName, BlobClientOptions.Value);
                await outputBlobClient.DeleteIfExistsAsync().ConfigureAwait(false);

                await AnonymizeSingleBlobInNdJsonFormatAsync(inputBlobClient, outputBlobClient, blob.Name, inputBlobPrefix);
            }
        }

        private async Task AnonymizeSingleBlobInJsonFormatAsync(BlobClient inputBlobClient, BlockBlobClient outputBlobClient, string blobName, string inputFolderPrefix)
        {
            try
            {
                using Stream contentStream = await OperationExecutionHelper.InvokeWithTimeoutRetryAsync<Stream>(async () =>
                {
                    Stream contentStream = new MemoryStream();
                    await inputBlobClient.DownloadToAsync(contentStream).ConfigureAwait(false);
                    contentStream.Position = 0;

                    return contentStream;
                }, 
                TimeSpan.FromSeconds(FhirAzureConstants.DefaultBlockDownloadTimeoutInSeconds), 
                FhirAzureConstants.DefaultBlockDownloadTimeoutRetryCount,
                isRetrableException: OperationExecutionHelper.IsRetrableException).ConfigureAwait(false);
                
                using (var reader = new StreamReader(contentStream))
                {
                    string input = await reader.ReadToEndAsync();
                    var engine = AnonymizerEngine.CreateWithFileContext(_configFile, blobName, inputFolderPrefix);
                    var settings = new AnonymizerSettings()
                    {
                        IsPrettyOutput = true
                    };
                    string output = engine.AnonymizeJson(input, settings);

                    using (MemoryStream outputStream = new MemoryStream(reader.CurrentEncoding.GetBytes(output)))
                    {
                        await OperationExecutionHelper.InvokeWithTimeoutRetryAsync(async () =>
                        {
                            outputStream.Position = 0;
                            using MemoryStream stream = new MemoryStream();
                            await outputStream.CopyToAsync(stream).ConfigureAwait(false);
                            stream.Position = 0;

                            return await outputBlobClient.UploadAsync(stream).ConfigureAwait(false);
                        }, 
                        TimeSpan.FromSeconds(FhirAzureConstants.DefaultBlockUploadTimeoutInSeconds), 
                        FhirAzureConstants.DefaultBlockUploadTimeoutRetryCount,
                        isRetrableException: OperationExecutionHelper.IsRetrableException).ConfigureAwait(false);
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

        private async Task AnonymizeSingleBlobInNdJsonFormatAsync(BlobClient inputBlobClient, BlockBlobClient outputBlobClient, string blobName, string inputFolderPrefix)
        {
            var processedCount = 0;
            var processedErrorCount = 0;
            var consumedCount = 0;

            using FhirBlobDataStream inputStream = new FhirBlobDataStream(inputBlobClient);
            FhirStreamReader reader = new FhirStreamReader(inputStream);
            FhirBlobConsumer consumer = new FhirBlobConsumer(outputBlobClient);
            var engine = AnonymizerEngine.CreateWithFileContext(_configFile, blobName, inputFolderPrefix);
            Func<string, string> anonymizerFunction = (item) =>
            {
                try
                {
                    return engine.AnonymizeJson(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{blobName}]: Anonymize partial failed, you can find detail error message in stderr.txt.");
                    Console.Error.WriteLine($"[{blobName}]: Resource: {item}\nErrorMessage: {ex.Message}\n Details: {ex.ToString()}\nStackTrace: {ex.StackTrace}");
                    throw;
                }
            };

            Stopwatch stopWatch = Stopwatch.StartNew();
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, consumer, anonymizerFunction);
            executor.PartitionCount = Environment.ProcessorCount * 2;
            Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
            progress.ProgressChanged += (obj, args) =>
            {
                Interlocked.Add(ref processedCount, args.ProcessCompleted);
                Interlocked.Add(ref processedErrorCount, args.ProcessFailed);
                Interlocked.Add(ref consumedCount, args.ConsumeCompleted);

                Console.WriteLine($"[{stopWatch.Elapsed.ToString()}][tid:{args.CurrentThreadId}]: {processedCount} Completed. {processedErrorCount} Failed. {consumedCount} consume completed.");
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

        public async Task Run(bool force = false)
        {
            // Increase connection limit of single endpoint: 2 => 128
            System.Net.ServicePointManager.DefaultConnectionLimit = 128;
            AnonymizerEngine.InitFhirPathExtensionSymbols();

            var input = LoadActivityInput();
            await AnonymizeDataset(input, force).ConfigureAwait(false);
        }
    }
}
