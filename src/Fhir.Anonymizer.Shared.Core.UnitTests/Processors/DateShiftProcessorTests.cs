using System;
using MicrosoftFhir.Anonymizer.Core.Models;
using MicrosoftFhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace MicrosoftFhir.Anonymizer.Core.UnitTests.Processors
{
    public class DateShiftProcessorTests
    {
        [Fact]
        public void GivenADateNode_WhenDateShift_DateShiftedNodeShouldBeReturned()
        {
            DateShiftProcessor processor = new DateShiftProcessor(dateShiftKey: "dummy", string.Empty, enablePartialDatesForRedact: true);
            Date testDate = new Date("2015-02-07");
            var node = ElementNode.FromElement(testDate.ToTypedElement());
            var processResult = processor.Process(node);
            Assert.Equal("2015-01-17", node.Value.ToString());
            Assert.True(processResult.IsPerturbed);

            testDate = new Date("2015-02");
            node = ElementNode.FromElement(testDate.ToTypedElement());
            processResult = processor.Process(node);
            Assert.Equal("2015", node.Value.ToString());
            Assert.True(processResult.IsRedacted);

            processor = new DateShiftProcessor(dateShiftKey: "dummy", string.Empty, enablePartialDatesForRedact: false);
            node = ElementNode.FromElement(testDate.ToTypedElement());
            processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);
        }

        [Fact]
        public void GivenADateTimeNode_WhenDateShift_DateShiftedNodeShouldBeReturn()
        {
            DateShiftProcessor processor = new DateShiftProcessor(dateShiftKey: "dummy", string.Empty, enablePartialDatesForRedact: true);
            FhirDateTime testDateTime = new FhirDateTime("2015-02-07T13:28:17-05:00");
            var node = ElementNode.FromElement(testDateTime.ToTypedElement());
            var processResult = processor.Process(node);
            Assert.Equal("2015-01-17T00:00:00-05:00", node.Value.ToString());
            Assert.True(processResult.IsPerturbed);
        }

        [Fact]
        public void GivenAInstantNode_WhenDateShift_DateShiftedNodeShouldBeReturn()
        {
            DateShiftProcessor processor = new DateShiftProcessor(dateShiftKey: "dummy", string.Empty, enablePartialDatesForRedact: true);
            Instant testInstant = new Instant(new DateTimeOffset(new DateTime(2015, 2, 7, 1, 1, 1, DateTimeKind.Utc)));
            var node = ElementNode.FromElement(testInstant.ToTypedElement());
            var processResult = processor.Process(node);
            Assert.Equal("2015-01-17T00:00:00+00:00", node.Value.ToString());
            Assert.True(processResult.IsPerturbed);
        }
    }
}
