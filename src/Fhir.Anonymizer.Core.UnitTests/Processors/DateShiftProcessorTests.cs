using System;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class DateShiftProcessorTests
    {
        [Fact]
        public void GivenADateNode_WhenDateShift_DateShiftedNodeShouldBeReturned()
        {
            DateShiftProcessor processor = new DateShiftProcessor(dateShiftKey: "dummy", enablePartialDatesForRedact: true);
            Date testDate = new Date("2015-02-07");
            var node = ElementNode.FromElement(testDate.ToTypedElement());
            processor.Process(node);
            Assert.Equal("2015-01-17", node.Value.ToString());

            testDate = new Date("2015-02");
            node = ElementNode.FromElement(testDate.ToTypedElement());
            processor.Process(node);
            Assert.Equal("2015", node.Value.ToString());

            processor = new DateShiftProcessor(dateShiftKey: "dummy", enablePartialDatesForRedact: false);
            node = ElementNode.FromElement(testDate.ToTypedElement());
            processor.Process(node);
            Assert.Null(node.Value);
        }

        [Fact]
        public void GivenADateTimeNode_WhenDateShift_DateShiftedNodeShouldBeReturn()
        {
            DateShiftProcessor processor = new DateShiftProcessor(dateShiftKey: "dummy", enablePartialDatesForRedact: true);
            FhirDateTime testDateTime = new FhirDateTime("2015-02-07T13:28:17-05:00");
            var node = ElementNode.FromElement(testDateTime.ToTypedElement());
            processor.Process(node);
            Assert.Equal("2015-01-17T00:00:00-05:00", node.Value.ToString());
        }

        [Fact]
        public void GivenAInstantNode_WhenDateShift_DateShiftedNodeShouldBeReturn()
        {
            DateShiftProcessor processor = new DateShiftProcessor(dateShiftKey: "dummy", enablePartialDatesForRedact: true);
            Instant testInstant = new Instant(new DateTimeOffset(new DateTime(2015, 2, 7, 1, 1, 1, DateTimeKind.Utc)));
            var node = ElementNode.FromElement(testInstant.ToTypedElement());
            processor.Process(node);
            Assert.Equal("2015-01-17T00:00:00+00:00", node.Value.ToString());
        }
    }
}
