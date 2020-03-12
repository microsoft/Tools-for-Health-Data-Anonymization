using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.PartitionedExecution;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.PartitionedExecution
{
    public class FhirStreamConsumerTests
    {
        [Fact]
        public async Task GivenAFhirStreamConsumer_WhenConsumeData_ShouldReadAllDataFromStream()
        {
            using MemoryStream outputStream = new MemoryStream();
            using FhirStreamConsumer consumer = new FhirStreamConsumer(outputStream);

            await consumer.ConsumeAsync(new List<string>() { "abc", "bcd", ""});
            await consumer.CompleteAsync();

            outputStream.Position = 0;
            using StreamReader reader = new StreamReader(outputStream);
            Assert.Equal("abc", await reader.ReadLineAsync());
            Assert.Equal("bcd", await reader.ReadLineAsync());
            Assert.Equal("", await reader.ReadLineAsync());
            Assert.Null(await reader.ReadLineAsync());
        }
    }
}
