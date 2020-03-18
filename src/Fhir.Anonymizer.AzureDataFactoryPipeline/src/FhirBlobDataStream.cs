using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Fhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public class FhirBlobDataStream : Stream
    {
        private BlobClient _blobClient;
        private Lazy<long> _blobLength;
        private Queue<Task<Stream>> _downloadTasks;
        private long _position;

        public FhirBlobDataStream(BlobClient blobClient)
        {
            _blobClient = blobClient;

            _blobLength = new Lazy<long>(() =>  _blobClient.GetProperties().Value.ContentLength);
            _downloadTasks = new Queue<Task<Stream>>();
            _position = 0;
        }

        public int ConcurrentCount
        {
            get;
            set;
        } = FhirAzureConstants.DefaultConcurrentCount;

        public int BlockDownloadTimeoutInSeconds
        {
            get;
            set;
        } = FhirAzureConstants.DefaultBlockDownloadTimeoutInSeconds;

        public int BlockDownloadTimeoutRetryCount
        {
            get;
            set;
        } = FhirAzureConstants.DefaultBlockDownloadTimeoutRetryCount;

        public Func<BlobClient, HttpRange, Task<Stream>> DownloadDataFunc =
            async (client, range) =>
            {
                using BlobDownloadInfo blobDownloadInfo = await client.DownloadAsync(range).ConfigureAwait(false);
                MemoryStream stream = new MemoryStream();
                await blobDownloadInfo.Content.CopyToAsync(stream).ConfigureAwait(false);
                stream.Position = 0;
                return stream;
            };

        public override bool CanRead => true;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (count > 0)
            {
                TryStartNewDownloadTask();

                if (_downloadTasks.Count > 0)
                {
                    Task<Stream> downloadTask = _downloadTasks.Peek();
                    downloadTask.Wait();

                    Stream contentStream = downloadTask.Result;
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

        private int TryStartNewDownloadTask()
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

        private async Task<Stream> DownloadBlobAsync(HttpRange range)
        {
            return await ExecutionWithTimeoutRetry.InvokeAsync<Stream>(async () =>
            {
                return await DownloadDataFunc(_blobClient, range).ConfigureAwait(false);
            }, timeout: TimeSpan.FromSeconds(BlockDownloadTimeoutInSeconds), BlockDownloadTimeoutRetryCount).ConfigureAwait(false);
        }

        private HttpRange NextRange()
        {
            long totalLength = _blobLength.Value;
            long? length = null;
            if (totalLength > _position)
            {
                length = Math.Min(totalLength - _position, FhirAzureConstants.BlockBufferSize);
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
