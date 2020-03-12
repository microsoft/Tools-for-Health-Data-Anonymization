using System;
using System.Collections.Generic;
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
            Progress<BatchAnonymizeResult> progress = new Progress<BatchAnonymizeResult>();
            progress.ProgressChanged += (obj, args) =>
            {
                Interlocked.Add(ref totalCount, args.Complete);
            };
            await executor.ExecuteAsync(CancellationToken.None, progress: progress);

            Assert.Equal(itemCount, totalCount);
            Assert.Equal(itemCount, testConsumer.CurrentOffset);
            Assert.Equal(99, testConsumer.BatchCount);
        }

        [Fact]
        public async Task GivenAPartitionedExecutor_WhenCancelled_OperationCancelledExceptionShouldBeThrow()
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
    }

    internal class TestFhirDataConsumer : IFhirDataConsumer
    {
        public int ItemCount { set; get; }

        public int BatchSize { set; get; }

        public int CurrentOffset { set; get; } = 0;

        public int BatchCount { set; get; } = 0;

        public TestFhirDataConsumer(int itemCount)
        {
            ItemCount = itemCount;
            CurrentOffset = 0;
            BatchCount = 0;
        }

        public async Task ConsumeAsync(IEnumerable<string> data)
        {
            BatchCount++;
            foreach (string content in data)
            {
                Assert.Equal(CurrentOffset++.ToString(), content);
            }
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

            return (CurrentOffset++).ToString();
        }
    }
}
