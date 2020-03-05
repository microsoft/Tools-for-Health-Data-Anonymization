using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core
{
    public class FhirPartitionedExecutor
    {
        public IFhirDataReader RawDataReader { set; get; }

        public IFhirDataConsumer AnonymizedDataConsumer { set; get; }

        public Func<string, string> AnonymizerFunction { set; get; }

        public int PartitionCount { set; get; } = Constants.DefaultPartitionedExecutionCount;

        public int BatchSize { set; get; } = Constants.DefaultPartitionedExecutionBatchSize;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Queue<Task<IEnumerable<string>>> executionTasks = new Queue<Task<IEnumerable<string>>>();
            List<string> batchData = new List<string>();

            string content;
            while ((content = await RawDataReader.NextAsync().ConfigureAwait(false)) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                batchData.Add(content);
                if (batchData.Count < BatchSize)
                {
                    continue;
                }

                executionTasks.Enqueue(AnonymizeAsync(batchData, cancellationToken));
                batchData = new List<string>();
                if (executionTasks.Count < PartitionCount)
                {
                    continue;
                }

                await ConsumeExecutionResultTask(executionTasks).ConfigureAwait(false);
            }

            if (batchData.Count > 0)
            {
                executionTasks.Enqueue(AnonymizeAsync(batchData, cancellationToken));
            }

            while (executionTasks.Count > 0)
            {
                await ConsumeExecutionResultTask(executionTasks).ConfigureAwait(false);
            }

            await AnonymizedDataConsumer.CompleteAsync().ConfigureAwait(false);
        }

        private async Task<IEnumerable<string>> AnonymizeAsync(List<string> batchData, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                List<string> result = new List<string>();
                foreach (string content in batchData)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                    result.Add(AnonymizerFunction(content));
                }

                return result;
            }).ConfigureAwait(false);
        }

        private async Task ConsumeExecutionResultTask(Queue<Task<IEnumerable<string>>> executionTasks)
        {
            IEnumerable<string> resultContents = await executionTasks.Dequeue();

            await AnonymizedDataConsumer.ConsumeAsync(resultContents).ConfigureAwait(false);
        }
    }
}
