using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.PartitionedExecution
{
    public class FhirPartitionedExecutionTests
    {
        [Fact] 
        public async Task GivenAPartitionedExecutor_WhenExecute_ResultShouldBeReturnedInOrder()
        {
            int itemCount = 9873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(new TestFhirDataReader(itemCount), testConsumer, (content) => content)
            {
                BatchSize = 100,
                PartitionCount = 10
            };

            int totalCount = 0;
            int consumeCount = 0;
            Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
            progress.ProgressChanged += (obj, args) =>
            {
                Interlocked.Add(ref totalCount, args.ProcessCompleted);
                Interlocked.Add(ref consumeCount, args.ConsumeCompleted);
            };
            await executor.ExecuteAsync(CancellationToken.None, progress: progress);

            Assert.Equal(itemCount, testConsumer.CurrentOffset);
            Assert.Equal(99, testConsumer.BatchCount);

            // Progress report is triggered by event, wait 1 second here in case progress not report.
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(itemCount, totalCount);
            Assert.Equal(itemCount, consumeCount);
        }

        [Fact]
        public async Task GivenAPartitionedExecutorNotKeepOrder_WhenExecute_AllResultShouldBeReturned()
        {
            int itemCount = 29873;
            var testConsumer = new TestFhirDataConsumer(itemCount)
            {
                CheckOrder = false
            };

            Random random = new Random();
            Func<string, Task<string>> anonymizeFunc = async content =>
            {
                if (random.Next() % 100 == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }

                return await Task.FromResult(content);
            };
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(new TestFhirDataReader(itemCount), testConsumer, anonymizeFunc)
            {
                BatchSize = 100,
                PartitionCount = 12,
                KeepOrder = false
            };

            int totalCount = 0;
            int consumeCount = 0;
            Progress<BatchAnonymizeProgressDetail> progress = new Progress<BatchAnonymizeProgressDetail>();
            progress.ProgressChanged += (obj, args) =>
            {
                Interlocked.Add(ref totalCount, args.ProcessCompleted);
                Interlocked.Add(ref consumeCount, args.ConsumeCompleted);
            };
            await executor.ExecuteAsync(CancellationToken.None, progress: progress);

            Assert.Equal(itemCount, testConsumer.CurrentOffset);
            Assert.Equal(299, testConsumer.BatchCount);

            // Progress report is triggered by event, wait 1 second here in case progress not report.
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(itemCount, totalCount);
            Assert.Equal(itemCount, consumeCount);
        }

        [Fact]
        public async Task GivenAPartitionedExecutor_WhenCancelled_OperationCancelledExceptionShouldBeThrown()
        {
            int itemCount = 9873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            var executor = new FhirPartitionedExecutor<string, string>(
                new TestFhirDataReader(itemCount), 
                testConsumer, 
                (content) =>
                {
                    Thread.Sleep(10);
                    return content;
                });

            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(1000);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await executor.ExecuteAsync(source.Token));
        }

        [Fact]
        public async Task GivenAPartitionedExecutorBreakOnExceptionEnabled_WhenExceptionThrow_ExecutionShouldStop()
        {
            int itemCount = 9873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            Func<string, string> invalidOperationFunc = (content) =>
            {
                throw new InvalidOperationException();
            };
            var executor = new FhirPartitionedExecutor<string, string>(
                new TestFhirDataReader(itemCount), 
                testConsumer,
                invalidOperationFunc);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.ExecuteAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GivenAPartitionedExecutor_WhenIOExceptionThrowFromReader_ExecutionShouldStop()
        {
            int itemCount = 91873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            var reader = new TestFhirDataReader(itemCount)
            {
                BreakOnOffset = 7431
            };
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, testConsumer, (content) => content);

            await Assert.ThrowsAsync<IOException>(async () => await executor.ExecuteAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GivenAPartitionedExecutor_WhenIOExceptionThrowFromConsumer_ExecutionShouldStop()
        {
            int itemCount = 91873;
            var testConsumer = new TestFhirDataConsumer(itemCount)
            {
                BreakOnOffset = 2342
            };
            var reader = new TestFhirDataReader(itemCount);
            FhirPartitionedExecutor<string, string> executor = new FhirPartitionedExecutor<string, string>(reader, testConsumer, (content) => content);

            await Assert.ThrowsAsync<IOException>(async () => await executor.ExecuteAsync(CancellationToken.None));
        }
    }

    internal class TestFhirDataConsumer : IFhirDataConsumer<string>
    {
        public int ItemCount { set; get; }

        public int BatchSize { set; get; }

        public int CurrentOffset { set; get; } = 0;

        public int BatchCount { set; get; } = 0;

        public int BreakOnOffset { set; get; } = -1;

        public bool CheckOrder { set; get; } = true;

        public TestFhirDataConsumer(int itemCount)
        {
            ItemCount = itemCount;
            CurrentOffset = 0;
            BatchCount = 0;
        }

        public async Task<int> ConsumeAsync(IEnumerable<string> data)
        {
            BatchCount++;
            int result = 0;
            foreach (string content in data)
            {
                if (CheckOrder)
                {
                    Assert.Equal(CurrentOffset.ToString(), content);
                }

                CurrentOffset++;
                result++;
                if (CurrentOffset == BreakOnOffset)
                {
                    throw new IOException();
                }
            }

            return result;
        }

        public async Task CompleteAsync()
        {
            Assert.Equal(ItemCount, CurrentOffset);
        }
    }

    internal class TestFhirDataReader : IFhirDataReader<string>
    {
        public int ItemCount { set; get; }

        private int CurrentOffset { set; get; }

        public int BreakOnOffset { set; get; } = -1;

        public TestFhirDataReader(int itemCount)
        {
            ItemCount = itemCount;
            CurrentOffset = 0;
        }

        public async Task<string> NextAsync()
        {
            if (CurrentOffset == ItemCount)
            {
                return null;
            }

            if (CurrentOffset == BreakOnOffset)
            {
                throw new IOException();
            }

            return (CurrentOffset++).ToString();
        }
    }
}
