using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class RedactProcessorTests
    {
        [Fact]
        public void GivenADateNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(enablePartialDatesForRedact: true, true, true, new List<string>());
            Date testDate = new Date("2015-02");
            var node = ElementNode.FromElement(testDate.ToTypedElement());
            processor.Process(node, new AnonymizationStatus());
            Assert.Equal("2015", node.Value.ToString());

            processor = new RedactProcessor(enablePartialDatesForRedact: false, true, true, new List<string>());
            node = ElementNode.FromElement(testDate.ToTypedElement());
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);
        }

        [Fact]
        public void GivenADateTimeNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(enablePartialDatesForRedact: true, true, true, new List<string>());
            FhirDateTime testDateTime = new FhirDateTime("2015-02-07T13:28:17-05:00");
            var node = ElementNode.FromElement(testDateTime.ToTypedElement());
            processor.Process(node, new AnonymizationStatus());
            Assert.Equal("2015", node.Value.ToString());

            processor = new RedactProcessor(enablePartialDatesForRedact: false, true, true, new List<string>());
            node = ElementNode.FromElement(testDateTime.ToTypedElement());
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);
        }

        [Fact]
        public void GivenAInstantNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(enablePartialDatesForRedact: true, true, true, new List<string>());
            Instant testInstant = new Instant(new DateTimeOffset(new DateTime(2015, 1, 1)));
            var node = ElementNode.FromElement(testInstant.ToTypedElement());
            processor.Process(node, new AnonymizationStatus());
            Assert.Equal("2015", node.Value.ToString());

            processor = new RedactProcessor(enablePartialDatesForRedact: false, true, true, new List<string>());
            node = ElementNode.FromElement(testInstant.ToTypedElement());
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);
        }

        [Fact]
        public void GivenAnAgeNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(true, enablePartialAgesForRedact: true, true, new List<string>());
            var age = new Age() { Value = 91 };
            var node = ElementNode.FromElement(age.ToTypedElement()).Children("value").Cast<ElementNode>().FirstOrDefault();
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);

            processor = new RedactProcessor(true, enablePartialAgesForRedact: false, true, new List<string>());
            node = ElementNode.FromElement(age.ToTypedElement()).Children("value").Cast<ElementNode>().FirstOrDefault();
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);

            processor = new RedactProcessor(true, enablePartialAgesForRedact: true, true, new List<string>());
            age = new Age() { Value = 89 };
            node = ElementNode.FromElement(age.ToTypedElement()).Children("value").Cast<ElementNode>().FirstOrDefault();
            processor.Process(node, new AnonymizationStatus());
            Assert.Equal("89", node.Value.ToString());
        }

        [Fact]
        public void GivenAPostalCodeNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(true, true, enablePartialZipCodesForRedact: true, restrictedZipCodeTabulationAreas: new List<string>() { "123" });
            var node = ElementNode.FromElement(new FhirString("12345").ToTypedElement());
            node.Name = "postalCode";
            processor.Process(node, new AnonymizationStatus());
            Assert.Equal("00000", node.Value.ToString());

            node = ElementNode.FromElement(new FhirString("54321").ToTypedElement());
            node.Name = "postalCode";
            processor.Process(node, new AnonymizationStatus());
            Assert.Equal("54300", node.Value.ToString());

            processor = new RedactProcessor(true, true, enablePartialZipCodesForRedact: false, restrictedZipCodeTabulationAreas: new List<string>() { });
            node = ElementNode.FromElement(new FhirString("54321").ToTypedElement());
            node.Name = "postalCode";
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);
        }

        [Fact]
        public void GivenAnOtherNode_WhenRedact_RedactedNodeShouldBeReturn()
        {
            RedactProcessor processor = new RedactProcessor(true, true, true, new List<string>());
            var node = ElementNode.FromElement(new FhirString("TestString").ToTypedElement());
            node.Name = "dummy";
            processor.Process(node, new AnonymizationStatus());
            Assert.Null(node.Value);
        }
    }
}
