// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Batch
{
    public abstract class BatchProcessor<TSource, TResult>
    {
        public int MaxBatchSize { get; set; } = 1000;

        public int ConcurrentCount { get; set; } = 3;

        public int OutputChannelLimit { get; set; } = 100;

        public (Channel<TResult> outputChannel, Task processTask) Process(Channel<TSource> inputChannel, CancellationToken cancellationToken)
        {
            Channel<TResult> outputChannel = Channel.CreateBounded<TResult>(OutputChannelLimit);

            Task processTask = Task.Run(
                async () =>
                {
                    await ProcessInternalAsync(inputChannel, outputChannel, cancellationToken);
                },
                cancellationToken);

            return (outputChannel, processTask);
        }

        public abstract TResult[] BatchProcessFunc(BatchInput<TSource> input);

        protected virtual async Task ProcessInternalAsync(Channel<TSource> inputChannel, Channel<TResult> outputChannel, CancellationToken cancellationToken)
        {
            int index = 0;

            try
            {
                List<TSource> buffer = new List<TSource>();
                List<Task<TResult[]>> runningTasks = new List<Task<TResult[]>>();

                await foreach (TSource resource in inputChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (buffer.Count < MaxBatchSize)
                    {
                        buffer.Add(resource);
                        continue;
                    }

                    while (runningTasks.Count >= ConcurrentCount)
                    {
                        TResult[] results = await runningTasks.First();
                        foreach (TResult result in results)
                        {
                            await outputChannel.Writer.WriteAsync(result, cancellationToken);
                        }
                    }

                    runningTasks.Add(Task.Run(() => BatchProcessFunc(new BatchInput<TSource>() { StartIndex = index, Sources = buffer.ToArray() })));
                    index += buffer.Count();
                    buffer.Clear();
                }

                runningTasks.Add(Task.Run(() => BatchProcessFunc(new BatchInput<TSource>() { StartIndex = index, Sources = buffer.ToArray() })));

                while (runningTasks.Count > 0)
                {
                    TResult[] results = await runningTasks.First();
                    runningTasks.RemoveAt(0);
                    foreach (TResult result in results)
                    {
                        await outputChannel.Writer.WriteAsync(result, cancellationToken);
                    }
                }
            }
            finally
            {
                outputChannel.Writer.Complete();
            }
        }
    }
}