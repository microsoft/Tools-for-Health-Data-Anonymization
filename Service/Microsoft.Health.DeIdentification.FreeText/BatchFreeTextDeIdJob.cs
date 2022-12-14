using Microsoft.Health.DeIdentification.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class JobInfo
    {

    }

    public class BatchFreeTextDeIdJob
    {
        // reuse job management lib

        public async Task<string> ExecuteAsync(JobInfo jobInfo, IProgress<string> progress, CancellationToken cancellationToken)
        {
            DataLoader<string> dataLoader = null; // Use job info to create data loader
            FreeTextDeIdBatchProcessor freeTextDeIdBatchProcessor = new FreeTextDeIdBatchProcessor(null);
            DataWriter<string, string> writer = null;

            (Channel<string> inputChannel, Task loadTask) = dataLoader.Load(cancellationToken);
            // It might be more steps here for different batch size and concurrent count. e.g. parsing text to fhir, learning model batch processing
            (Channel<string> processChannel, Task processTask) = freeTextDeIdBatchProcessor.Process(inputChannel, cancellationToken);
            (Channel<string> resultChannel, Task writeTask) = writer.Process(processChannel, cancellationToken);

            await foreach (string batchProgress in resultChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                progress.Report(batchProgress);
            }

            try
            {
                await writeTask;
                await processTask;
                await loadTask;
            }
            catch
            {
                // log here
                throw;
            }

            return string.Empty; // result string
        }
    }
}
