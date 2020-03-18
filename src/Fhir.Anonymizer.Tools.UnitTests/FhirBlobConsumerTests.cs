using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Fhir.Anonymizer.AzureDataFactoryPipeline.src;
using Xunit;

namespace Fhir.Anonymizer.Tools.UnitTests
{
    public class FhirBlobConsumerTests
    {
        [Fact(Skip = "StorageEmulatorSupportOnly")]
        public async Task GivenAFhirBlobConsumer_WhenConsumeData_AllDataShouldbeUploaded()
        {
            string containerName = Guid.NewGuid().ToString("N");
            string blobName = Guid.NewGuid().ToString("N");
            string connectionString = "UseDevelopmentStorage=true";
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync();
            try
            {
                BlockBlobClient blobClient = new BlockBlobClient(connectionString, containerName, blobName);
                await blobClient.DeleteIfExistsAsync();

                FhirBlobConsumer consumer = new FhirBlobConsumer(blobClient);
                long totalSize = 0;
                Progress<long> progress = new Progress<long>();
                progress.ProgressChanged += (obj, args) =>
                {
                    Interlocked.Add(ref totalSize, args);
                };
                consumer.ProgressHandler = progress;
                int seed = DateTime.Now.Second % 30;
                foreach (var batch in GenerateTestData(20, 10000, seed))
                {
                    await consumer.ConsumeAsync(batch);
                }

                await consumer.CompleteAsync();

                using StreamReader reader = new StreamReader((await blobClient.DownloadAsync()).Value.Content);
                foreach (var batch in GenerateTestData(20, 10000, seed))
                {
                    foreach (var item in batch)
                    {
                        Assert.Equal(item, await reader.ReadLineAsync());
                    }
                }
                Assert.Null(await reader.ReadLineAsync());
                Assert.Equal((await blobClient.GetPropertiesAsync()).Value.ContentLength, totalSize);
            }
            finally
            {
                await containerClient.DeleteIfExistsAsync();
            }
        }

        private static IEnumerable<List<string>> GenerateTestData(int batchCount, int itemCountInBatch, int seed)
        {
            Random random = new Random(seed);
            
            while (batchCount-- > 0)
            {
                List<string> result = new List<string>();
                int lines = 0;

                while (lines++ < itemCountInBatch)
                {
                    string content = new string('*', random.Next(2, 1024 * 4)) + "aA!1·\t中";
                    result.Add(content);
                }

                yield return result;
            }
        }
    }
}
