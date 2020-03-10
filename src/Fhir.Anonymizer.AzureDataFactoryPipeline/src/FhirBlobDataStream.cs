using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Fhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public class FhirBlobDataStream : Stream
    {
        private const int KB = 1024;
        private const int MB = KB * 1024;
        private const int BlockBufferSize = 4 * MB;
        private const int DefaultConcurrentCount = 3;
        private const int DefaultBlockDownloadTimeoutInSeconds = 5 * 60;
        private const int DefaultBlockDownloadTimeoutRetryCount = 3;
        

        private BlobClient _blobClient;
        private Lazy<long> _blobLength;
        private Queue<Task<BlobDownloadInfo>> _downloadTasks;
        private long _position;

        public FhirBlobDataStream(BlobClient blobClient)
        {
            _blobClient = blobClient;

            _blobLength = new Lazy<long>(() =>  _blobClient.GetProperties().Value.ContentLength);
            _downloadTasks = new Queue<Task<BlobDownloadInfo>>();
            _position = 0;
        }

        public int ConcurrentCount
        {
            get;
            set;
        } = DefaultConcurrentCount;

        public int BlockDownloadTimeoutInSeconds
        {
            get;
            set;
        } = DefaultBlockDownloadTimeoutInSeconds;

        public int BlockDownloadTimeoutRetryCount
        {
            get;
            set;
        } = DefaultBlockDownloadTimeoutRetryCount;

        public Func<BlobClient, HttpRange, Task<BlobDownloadInfo>> DownloadDataFunc =
            async (client, range) =>
            {
                return await client.DownloadAsync(range).ConfigureAwait(false);
            };

        public override bool CanRead => true;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (count > 0)
            {
                StartNewDownloadTask();

                if (_downloadTasks.Count > 0)
                {
                    Task<BlobDownloadInfo> downloadTask = _downloadTasks.Peek();
                    downloadTask.Wait();

                    Stream contentStream = downloadTask.Result.Content;
                    int bytesRead = contentStream.Read(buffer, offset, count);
                    if (bytesRead == 0)
                    {
                        _downloadTasks.Dequeue().Dispose();
                        continue;
                    }

                    totalBytesRead += bytesRead;
                    offset += bytesRead;
                    count -= bytesRead;
                }
                else
                {
                    break;
                }
            }            

            return totalBytesRead;
        }

        private int StartNewDownloadTask()
        {
            int newTasksStarted = 0;
            while (_downloadTasks.Count < ConcurrentCount)
            {
                HttpRange nextRange = NextRange();
                if ((nextRange.Length ?? 0) == 0) // the range is empty => all data downloaded.
                {
                    break;
                }

                _downloadTasks.Enqueue(DownloadBlobAsync(nextRange));
                _position += nextRange.Length ?? 0;
                newTasksStarted++;
            }

            return newTasksStarted;
        }

        private async Task<BlobDownloadInfo> DownloadBlobAsync(HttpRange range)
        {
            return await ExecutionWithTimeoutRetry.InvokeAsync<BlobDownloadInfo>(async () =>
            {
                return await DownloadDataFunc(_blobClient, range).ConfigureAwait(false);
            }, timeout: TimeSpan.FromSeconds(BlockDownloadTimeoutInSeconds), BlockDownloadTimeoutRetryCount);
        }

        private HttpRange NextRange()
        {
            long totalLength = _blobLength.Value;
            long? length = null;
            if (totalLength > _position)
            {
                length = Math.Min(totalLength - _position, BlockBufferSize);
            }

            return new HttpRange(_position, length);
        }

        #region Not implemented for non seekable operation
        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
