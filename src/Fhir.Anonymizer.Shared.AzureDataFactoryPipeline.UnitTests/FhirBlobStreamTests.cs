using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Health.Fhir.Anonymizer.AzureDataFactoryPipeline.src;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Tools.UnitTests
{
    public class FhirBlobStreamTests
    {
        [Fact(Skip = "StorageEmulatorSupportOnly")]
        public async Task GivenAFhirBlobStream_WhenDownloadData_AllDataShouldbeReturned()
        {
            string containerName = Guid.NewGuid().ToString("N");
            string blobName = Guid.NewGuid().ToString("N");
            BlobContainerClient containerClient = new BlobContainerClient("UseDevelopmentStorage=true", containerName);
            
            try
            {
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(blobName);

                List<string> expectedResult = await GenerateTestBlob(blobClient);

                FhirBlobDataStream stream = new FhirBlobDataStream(blobClient);
                StreamReader reader = new StreamReader(stream);
                for (int i = 0; i < expectedResult.Count; ++i)
                {
                    var content = await reader.ReadLineAsync();
                    Assert.Equal(expectedResult[i], content);
                }
            }
            finally
            {
                await containerClient.DeleteIfExistsAsync();
            }
        }

        [Fact(Skip = "StorageEmulatorSupportOnly")]
        public async Task GivenAFhirBlobStream_WhenDownloadDataTimeout_OperationShouldBeRetried()
        {
            string containerName = Guid.NewGuid().ToString("N");
            string blobName = Guid.NewGuid().ToString("N");
            BlobContainerClient containerClient = new BlobContainerClient("UseDevelopmentStorage=true", containerName);

            try
            {
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(blobName);

                List<string> expectedResult = await GenerateTestBlob(blobClient);

                FhirBlobDataStream stream = new FhirBlobDataStream(blobClient);
                Dictionary<long, int> enterRecord = new Dictionary<long, int>();
                stream.BlockDownloadTimeoutRetryCount = 1;
                stream.BlockDownloadTimeoutInSeconds = 5;
                stream.DownloadDataFunc = async (client, range) =>
                {
                    if (!enterRecord.ContainsKey(range.Offset))
                    {
                        enterRecord[range.Offset] = 0;
                    }

                    if (enterRecord[range.Offset]++ < 1)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }

                    var downloadInfo = await client.DownloadAsync(range).ConfigureAwait(false);
                    return downloadInfo.Value.Content;
                };

                StreamReader reader = new StreamReader(stream);
                for (int i = 0; i < expectedResult.Count; ++i)
                {
                    var content = await reader.ReadLineAsync();
                    Assert.Equal(expectedResult[i], content);
                }

                foreach (int count in enterRecord.Values)
                {
                    Assert.Equal(2, count);
                }
            }
            finally
            {
                await containerClient.DeleteIfExistsAsync();
            }
        }

        private static async Task<List<string>> GenerateTestBlob(BlobClient blobClient)
        {
            Random random = new Random();
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream, encoding:Encoding.UTF8);
            int lines = 0;
            var expectedResult = new List<string>();
            while (lines++ < 1024)
            {
                string content = new string('*', random.Next(2, 1024 * 128)) + "aA!1·\t中";
                await writer.WriteLineAsync(content);
                expectedResult.Add(content);
            }

            writer.Flush();

            stream.Position = 0;
            await blobClient.UploadAsync(stream);
            return expectedResult;
        }
    }
}
