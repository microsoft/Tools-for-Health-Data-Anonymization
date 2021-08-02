using System.Collections.Generic;
using System.IO;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests
{
    public class EmptyElementTest
    {
        private static FhirJsonParser _parser = new FhirJsonParser();

        public static IEnumerable<object[]> EmptyElementFile()
        {
            yield return new object[] { "patient-empty.json"};
            yield return new object[] { "bundle-empty.json" };
            yield return new object[] { "condition-empty.json" };
        }

        public static IEnumerable<object[]> NonEmptyElementFile()
        {
            yield return new object[] { "contained-basic.json" };
            yield return new object[] { "bundle-basic.json" };
        }

        public static IEnumerable<object[]> NonEmptyElementContent()
        {
            yield return new object[] { null };
            yield return new object[] { "0" };
            yield return new object[] { "empty" };
            yield return new object[] { "{\"resourceType\":\"Patient\"}" };
        }

        [Theory]
        [MemberData(nameof(EmptyElementFile))]
        public void GivenEmptyElement_WhenCheckIFEmptyElement_ResultShouldBeTrue(string file)
        {
            var json = File.ReadAllText(Path.Join("TestResources", file));
            var element = _parser.Parse<Resource>(json).ToTypedElement();
            Assert.True(EmptyElement.IsEmptyElement(element));
            Assert.True(EmptyElement.IsEmptyElement((object)element));
        }

        [Theory]
        [MemberData(nameof(EmptyElementFile))]
        public void GivenEmptyElementJson_WhenCheckIFEmptyElement_ResultShouldBeTrue(string file)
        {
            var json = File.ReadAllText(Path.Join("TestResources", file));
            Assert.True(EmptyElement.IsEmptyElement(json));
            Assert.True(EmptyElement.IsEmptyElement((object)json));
        }

        [Theory]
        [MemberData(nameof(NonEmptyElementFile))]
        public void GivenNonEmptyElementJson_WhenCheckIFEmptyElement_ResultShouldBeFalse(string file)
        {
            var json = File.ReadAllText(Path.Join("TestResources", file));
            Assert.False(EmptyElement.IsEmptyElement(json));
            Assert.False(EmptyElement.IsEmptyElement((object)json));
        }

        [Theory]
        [MemberData(nameof(NonEmptyElementFile))]
        public void GivenNonEmptyElement_WhenCheckIFEmptyElement_ResultShouldBeFalse(string file)
        {
            var json = File.ReadAllText(Path.Join("TestResources", file));
            var element = _parser.Parse<Resource>(json).ToTypedElement();
            Assert.False(EmptyElement.IsEmptyElement(json));
            Assert.False(EmptyElement.IsEmptyElement((object)json));
        }

        [Theory]
        [MemberData(nameof(NonEmptyElementContent))]
        public void GivenNonEmptyElemetContent_WhenCheckIFEmptyElement_ResultShouldBeFalse(string content)
        {
            Assert.False(EmptyElement.IsEmptyElement(content));
            Assert.False(EmptyElement.IsEmptyElement((object)content));
        }
    }
}
