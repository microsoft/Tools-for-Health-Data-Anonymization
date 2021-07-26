using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class RedactProcessorTests
    {
        [Fact]
        public void GivenADateNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(enablePartialDatesForRedact: true, true, true, new List<string>());
            Date testDate = new Date("2015-02");
            var node = ElementNode.FromElement(testDate.ToTypedElement());
            var processResult = processor.Process(node);
            Assert.Equal("2015", node.Value.ToString());
            Assert.True(processResult.IsRedacted);

            processor = new RedactProcessor(enablePartialDatesForRedact: false, true, true, new List<string>());
            node = ElementNode.FromElement(testDate.ToTypedElement());
            processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);
        }

        [Fact]
        public void GivenADateTimeNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(enablePartialDatesForRedact: true, true, true, new List<string>());
            FhirDateTime testDateTime = new FhirDateTime("2015-02-07T13:28:17-05:00");
            var node = ElementNode.FromElement(testDateTime.ToTypedElement());
            var processResult = processor.Process(node);
            Assert.Equal("2015", node.Value.ToString());
            Assert.True(processResult.IsRedacted);

            processor = new RedactProcessor(enablePartialDatesForRedact: false, true, true, new List<string>());
            node = ElementNode.FromElement(testDateTime.ToTypedElement());
            processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);
        }

        [Fact]
        public void GivenAInstantNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(enablePartialDatesForRedact: true, true, true, new List<string>());
            Instant testInstant = new Instant(new DateTimeOffset(new DateTime(2015, 1, 1)));
            var node = ElementNode.FromElement(testInstant.ToTypedElement());
            var processResult = processor.Process(node);
            Assert.Equal("2015", node.Value.ToString());
            Assert.True(processResult.IsRedacted);

            processor = new RedactProcessor(enablePartialDatesForRedact: false, true, true, new List<string>());
            node = ElementNode.FromElement(testInstant.ToTypedElement());
            processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);
        }

        [Fact]
        public void GivenAnAgeNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(true, enablePartialAgesForRedact: true, true, new List<string>());
            var age = new Age() { Value = 91 };

            // If an ElementNode is created by ElementNode.FromElement(), its children are of type ElementNode
            // Cast them to ElementNode directly
            // https://github.com/FirelyTeam/firely-net-common/blob/master/src/Hl7.Fhir.ElementModel/ElementNode.cs
            var node = ElementNode.FromElement(age.ToTypedElement()).Children("value").FirstOrDefault() as ElementNode;
            var processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);

            processor = new RedactProcessor(true, enablePartialAgesForRedact: false, true, new List<string>());
            node = ElementNode.FromElement(age.ToTypedElement()).Children("value").FirstOrDefault() as ElementNode;
            processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);

            processor = new RedactProcessor(true, enablePartialAgesForRedact: true, true, new List<string>());
            age = new Age() { Value = 89 };
            node = ElementNode.FromElement(age.ToTypedElement()).Children("value").FirstOrDefault() as ElementNode;
            processResult = processor.Process(node);
            Assert.Equal("89", node.Value.ToString());
            Assert.True(processResult.IsRedacted);
        }

        [Fact]
        public void GivenAPostalCodeNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(true, true, enablePartialZipCodesForRedact: true, restrictedZipCodeTabulationAreas: new List<string>() { "123" });
            var node = ElementNode.FromElement(new FhirString("12345").ToTypedElement());
            node.Name = "postalCode";
            var processResult = processor.Process(node);
            Assert.Equal("00000", node.Value.ToString());
            Assert.True(processResult.IsAbstracted);

            node = ElementNode.FromElement(new FhirString("54321").ToTypedElement());
            node.Name = "postalCode";
            processResult = processor.Process(node);
            Assert.Equal("54300", node.Value.ToString());
            Assert.True(processResult.IsAbstracted);

            processor = new RedactProcessor(true, true, enablePartialZipCodesForRedact: false, restrictedZipCodeTabulationAreas: new List<string>() { });
            node = ElementNode.FromElement(new FhirString("54321").ToTypedElement());
            node.Name = "postalCode";
            processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);
        }

        [Fact]
        public void GivenAnOtherNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(true, true, true, new List<string>());
            var node = ElementNode.FromElement(new FhirString("TestString").ToTypedElement());
            node.Name = "dummy";
            var processResult = processor.Process(node);
            Assert.Null(node.Value);
            Assert.True(processResult.IsRedacted);
        }
    }
}
