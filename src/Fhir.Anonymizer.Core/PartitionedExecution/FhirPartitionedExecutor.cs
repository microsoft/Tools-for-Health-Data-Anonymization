﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.PartitionedExecution;

namespace Fhir.Anonymizer.Core
{
    public class FhirPartitionedExecutor
    {
        public IFhirDataReader RawDataReader { set; get; }

        public IFhirDataConsumer AnonymizedDataConsumer { set; get; }

        public Func<string, string> AnonymizerFunction { set; get; }

        public int PartitionCount { set; get; } = Constants.DefaultPartitionedExecutionCount;

        public int BatchSize { set; get; } = Constants.DefaultPartitionedExecutionBatchSize;

        public FhirPartitionedExecutor(IFhirDataReader rawDataReader, IFhirDataConsumer anonymizedDataConsumer, Func<string, string> anonymizerFunction)
        {
            RawDataReader = rawDataReader;
            AnonymizedDataConsumer = anonymizedDataConsumer;
            AnonymizerFunction = anonymizerFunction;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken, bool breakOnAnonymizationException = false, IProgress<BatchAnonymizeProgressDetail> progress = null)
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

                executionTasks.Enqueue(AnonymizeAsync(batchData, breakOnAnonymizationException, progress, cancellationToken));
                batchData = new List<string>();
                if (executionTasks.Count < PartitionCount)
                {
                    continue;
                }

                await ConsumeExecutionResultTask(executionTasks, progress).ConfigureAwait(false);
            }

            if (batchData.Count > 0)
            {
                executionTasks.Enqueue(AnonymizeAsync(batchData, breakOnAnonymizationException, progress, cancellationToken));
            }

            while (executionTasks.Count > 0)
            {
                await ConsumeExecutionResultTask(executionTasks, progress).ConfigureAwait(false);
            }

            await AnonymizedDataConsumer.CompleteAsync().ConfigureAwait(false);
        }

        private async Task<IEnumerable<string>> AnonymizeAsync(List<string> batchData, bool breakOnAnonymizationException, IProgress<BatchAnonymizeProgressDetail> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                List<string> result = new List<string>();

                BatchAnonymizeProgressDetail batchAnonymizeProgressDetail = new BatchAnonymizeProgressDetail();
                batchAnonymizeProgressDetail.CurrentThreadId = Thread.CurrentThread.ManagedThreadId;
              
                foreach (string content in batchData)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }

                    try
                    {
                        string anonymizedResult = AnonymizerFunction(content);
                        result.Add(anonymizedResult);
                        batchAnonymizeProgressDetail.ProcessCompleted++;
                    }
                    catch (Exception)
                    {
                        if (breakOnAnonymizationException)
                        {
                            throw;
                        }

                        batchAnonymizeProgressDetail.ProcessFailed++;
                    }                    
                }

                progress?.Report(batchAnonymizeProgressDetail);
                return result;
            }).ConfigureAwait(false);
        }

        private async Task ConsumeExecutionResultTask(Queue<Task<IEnumerable<string>>> executionTasks, IProgress<BatchAnonymizeProgressDetail> progress)
        {
            IEnumerable<string> resultContents = await executionTasks.Dequeue().ConfigureAwait(false);

            int consumeCount = await AnonymizedDataConsumer.ConsumeAsync(resultContents).ConfigureAwait(false);
            progress?.Report(new BatchAnonymizeProgressDetail() { ConsumeCompleted = consumeCount, CurrentThreadId = Thread.CurrentThread.ManagedThreadId });
        }
    }
}
