using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.PartitionedExecution;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.PartitionedExecution
{
    public class FhirPartitionedExecutionTests
    {
        [Fact] 
        public async Task GivenAPartitionedExecutor_WhenExecute_ResultShouldBeReturnedInOrder()
        {
            int itemCount = 9873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            FhirPartitionedExecutor executor = new FhirPartitionedExecutor(new TestFhirDataReader(itemCount), testConsumer, (content) => content)
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

            Assert.Equal(itemCount, totalCount);
            Assert.Equal(itemCount, testConsumer.CurrentOffset);
            Assert.Equal(99, testConsumer.BatchCount);
            Assert.Equal(9873, consumeCount);
        }

        [Fact]
        public async Task GivenAPartitionedExecutor_WhenCancelled_OperationCancelledExceptionShouldBeThrown()
        {
            int itemCount = 9873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            FhirPartitionedExecutor executor = new FhirPartitionedExecutor(new TestFhirDataReader(itemCount), testConsumer, (content) => content);

            executor.AnonymizerFunction = (content) =>
            {
                Thread.Sleep(10);
                return content;
            };

            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(1000);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await executor.ExecuteAsync(source.Token));
        }

        [Fact]
        public async Task GivenAPartitionedExecutorBreakOnExceptionEnabled_WhenExceptionThrow_ExecutionShouldStop()
        {
            int itemCount = 9873;
            var testConsumer = new TestFhirDataConsumer(itemCount);
            FhirPartitionedExecutor executor = new FhirPartitionedExecutor(new TestFhirDataReader(itemCount), testConsumer, (content) => content);

            executor.AnonymizerFunction = (content) =>
            {
                throw new InvalidOperationException();
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.ExecuteAsync(CancellationToken.None, true));
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
            FhirPartitionedExecutor executor = new FhirPartitionedExecutor(reader, testConsumer, (content) => content);

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
            FhirPartitionedExecutor executor = new FhirPartitionedExecutor(reader, testConsumer, (content) => content);

            await Assert.ThrowsAsync<IOException>(async () => await executor.ExecuteAsync(CancellationToken.None));
        }
    }

    internal class TestFhirDataConsumer : IFhirDataConsumer
    {
        public int ItemCount { set; get; }

        public int BatchSize { set; get; }

        public int CurrentOffset { set; get; } = 0;

        public int BatchCount { set; get; } = 0;

        public int BreakOnOffset { set; get; } = -1;

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
                Assert.Equal((CurrentOffset++).ToString(), content);
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

    internal class TestFhirDataReader : IFhirDataReader
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
