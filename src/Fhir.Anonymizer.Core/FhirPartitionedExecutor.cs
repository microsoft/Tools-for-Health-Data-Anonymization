using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core
{
    public class FhirPartitionedExecutor
    {
        public IFhirDataReader RawDataReader { set; get; }

        public IFhirDataConsumer AnonymizedDataConsumer { set; get; }

        public Func<string, string> AnonymizerFunction { set; get; }

        public int PartitionCount { set; get; } = 5;

        public int BatchSize { set; get; } = 10000;

        public async Task ExecuteAsync()
        {
            Queue<Task<IEnumerable<string>>> executionTasks = new Queue<Task<IEnumerable<string>>>();
            List<string> batchData = new List<string>();

            while (RawDataReader.HasNext())
            {
                string content = RawDataReader.Next();
                batchData.Add(content);
                if (batchData.Count < BatchSize)
                {
                    continue;
                }

                executionTasks.Enqueue(AnonymizeAsync(batchData));
                batchData = new List<string>();
                if (executionTasks.Count < PartitionCount)
                {
                    continue;
                }

                await ConsumeExecutionResultTask(executionTasks).ConfigureAwait(false);
            }

            if (batchData.Count > 0)
            {
                executionTasks.Enqueue(AnonymizeAsync(batchData));
            }

            while (executionTasks.Count > 0)
            {
                await ConsumeExecutionResultTask(executionTasks).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<string>> AnonymizeAsync(List<string> batchData)
        {
            return await Task.Run(() =>
            {
                List<string> result = new List<string>();
                foreach (string content in batchData)
                {
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
