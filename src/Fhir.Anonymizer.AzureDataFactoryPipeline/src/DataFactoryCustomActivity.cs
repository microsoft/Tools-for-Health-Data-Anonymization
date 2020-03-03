using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fhir.Anonymizer.Core;
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
            var inputContainer = new BlobContainerClient(inputData.SourceStorageConnectionString, inputData.SourceContainerName.ToLower());
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

                var inputBlobClient = inputContainer.GetBlobClient(blob.Name);
                var outputBlobClient = outputContainer.GetBlobClient(outputBlobName);

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

        private async Task AnonymizeBlobInJsonFormatAsync(BlobClient inputBlobClient, BlobClient outputBlobClient, string blobName)
        {
            try
            {
                var inputDownloadInfo = await inputBlobClient.DownloadAsync();
                using (var reader = new StreamReader(inputDownloadInfo.Value.Content))
                {
                    string input = await reader.ReadToEndAsync();
                    string output = _engine.AnonymizeJson(input);

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

        private async Task AnonymizeBlobInNdJsonFormatAsync(BlobClient inputBlobClient, BlobClient outputBlobClient, string blobName)
        {
            var inputDownloadInfo = await inputBlobClient.DownloadAsync();
            var outputTempFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Guid.NewGuid().ToString()}");

            var processedCount = 0;
            var processedErrorCount = 0;
            using (FileStream destinationStream = File.Create(outputTempFileName))
            {
                using var reader = new StreamReader(inputDownloadInfo.Value.Content);
                using var writer = new StreamWriter(destinationStream, reader.CurrentEncoding);
                string resourceLine;
                string resultLine;

                while ((resourceLine = reader.ReadLine()) != null)
                {
                    try
                    {
                        resultLine = _engine.AnonymizeJson(resourceLine);
                        writer.WriteLine(resultLine);
                        processedCount += 1;
                    }
                    catch (Exception innerException)
                    {
                        processedErrorCount += 1;
                        Console.WriteLine($"[{blobName}]: Anonymize partial failed, you can find detail error message in stderr.txt.");
                        Console.Error.WriteLine($"[{blobName}]: Error #{processedErrorCount}\nResource: {resourceLine}\nErrorMessage: {innerException.Message}\n Details: {innerException.ToString()}\nStackTrace: {innerException.StackTrace}");
                    }
                }
                writer.Flush();
            }

            using (FileStream uploadFileStream = new FileStream(outputTempFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                await outputBlobClient.UploadAsync(uploadFileStream);
                Console.WriteLine($"[{blobName}]: Succeeded in {processedCount} resources, failed in {processedErrorCount} resources in total.");
            }
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

        public void Run(bool force = false)
        {
            _engine = new AnonymizerEngine("./configuration-sample.json");

            var input = LoadActivityInput();
            AnonymizeDataset(input, force).Wait();
        }
    }
}
