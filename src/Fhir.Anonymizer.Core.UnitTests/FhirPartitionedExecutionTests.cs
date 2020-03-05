using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
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
                PartitionCount = 19
            };

            await executor.ExecuteAsync(CancellationToken.None);

            Assert.Equal(itemCount, testConsumer.CurrentOffset);
            Assert.Equal(99, testConsumer.BatchCount);
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
