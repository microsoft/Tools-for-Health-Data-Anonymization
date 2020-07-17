using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Fhir.Anonymizer.Core;

namespace Fhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public class FhirBlobConsumer : IFhirDataConsumer<string>
    {
        private static byte[] _newLine = Encoding.UTF8.GetBytes("\r\n");

        private BlockBlobClient _blobClient;
        private List<Task> _runningTasks;
        private List<string> _blockIds;
        private MemoryStream _currentStream;

        public FhirBlobConsumer(BlockBlobClient blobClient)
        {
            _blobClient = blobClient;

            _runningTasks = new List<Task>();
            _blockIds = new List<string>();
            _currentStream = new MemoryStream();
        }

        public int UploadBlockThreshold
        {
            get;
            set;
        } = FhirAzureConstants.DefaultUploadBlockThreshold;

        public int ConcurrentCount
        {
            get;
            set;
        } = FhirAzureConstants.DefaultConcurrentCount;

        public IProgress<long> ProgressHandler
        {
            set;
            private get;
        } = null;

        public int BlockUploadTimeoutInSeconds
        {
            get;
            set;
        } = FhirAzureConstants.DefaultBlockUploadTimeoutInSeconds;

        public int BlockUploadTimeoutRetryCount
        {
            get;
            set;
        } = FhirAzureConstants.DefaultBlockUploadTimeoutRetryCount;

        public async Task<int> ConsumeAsync(IEnumerable<string> data)
        {
            int result = 0;
            foreach (string item in data)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(item);
                await _currentStream.WriteAsync(byteData, 0, byteData.Length).ConfigureAwait(false);
                await _currentStream.WriteAsync(_newLine, 0, _newLine.Length).ConfigureAwait(false);
                if (_currentStream.Length >= UploadBlockThreshold)
                {
                    await AddCurrentStreamToUploadTaskListAsync().ConfigureAwait(false);
                }

                result++;
            }

            return result;
        }

        public async Task CompleteAsync()
        {
            if (_currentStream.Length > 0)
            {
                await AddCurrentStreamToUploadTaskListAsync().ConfigureAwait(false);
            }

            await Task.WhenAll(_runningTasks).ConfigureAwait(false);
            foreach (var task in _runningTasks)
            {
                // If there's any error throw exception here.
                await task.ConfigureAwait(false);
            }

            await _blobClient.CommitBlockListAsync(_blockIds).ConfigureAwait(false);
        }

        private async Task AddCurrentStreamToUploadTaskListAsync()
        {
            while (_runningTasks.Count > ConcurrentCount)
            {
                await Task.WhenAny(_runningTasks).ConfigureAwait(false);

                foreach (var task in _runningTasks.FindAll(t => t.IsCompleted))
                {
                    // If there's any error throw exception here.
                    await task.ConfigureAwait(false);
                }

                _runningTasks.RemoveAll(t => t.IsCompleted);
            }

            string blockId = GenerateBlockId(_blockIds.Count);
            _runningTasks.Add(StageBlockAsync(blockId, _currentStream));
            _blockIds.Add(blockId);

            _currentStream = new MemoryStream();
        }

        private async Task StageBlockAsync(
            string blockId,
            MemoryStream stream)
        {
            await OperationExecutionHelper.InvokeWithTimeoutRetryAsync<BlockInfo>(
                async () =>
                {
                    using MemoryStream uploadStream = new MemoryStream();
                    stream.Position = 0;
                    await stream.CopyToAsync(uploadStream).ConfigureAwait(false);
                    uploadStream.Position = 0;

                    return await _blobClient.StageBlockAsync(blockId, uploadStream).ConfigureAwait(false);
                }, 
                TimeSpan.FromSeconds(BlockUploadTimeoutInSeconds), 
                BlockUploadTimeoutRetryCount,
                isRetrableException: OperationExecutionHelper.IsRetrableException).ConfigureAwait(false);

            ProgressHandler?.Report(stream.Length);
            await stream.DisposeAsync().ConfigureAwait(false);
        }

        private static string GenerateBlockId(int index)
        {
            byte[] id = new byte[48]; // 48 raw bytes => 64 byte string once Base64 encoded
            BitConverter.GetBytes(index).CopyTo(id, 0);
            return Convert.ToBase64String(id);
        }
    }
}
