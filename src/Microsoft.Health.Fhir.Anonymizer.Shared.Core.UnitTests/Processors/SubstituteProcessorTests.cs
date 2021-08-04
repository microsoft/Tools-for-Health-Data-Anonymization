using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class SubstituteProcessorTests
    {
        public static IEnumerable<object[]> GetPrimitiveNodes()
        {
            yield return new object[] { new FhirBoolean(true), "{ \"replaceWith\": null }" };
            yield return new object[] { new FhirBoolean(true), "{ \"replaceWith\": \"string.Empty\" }" };
            yield return new object[] { new FhirBoolean(true), "{ \"replaceWith\": false }" };
            yield return new object[] { new Id("123"), "{ \"replaceWith\": \"abc\" }" };
            yield return new object[] { new FhirDecimal(1), "{ \"replaceWith\": 1 }" };
            yield return new object[] { new Date("2000"), "{ \"replaceWith\": \"2000\" }" };
        }

        public static IEnumerable<object[]> GetComplexNodes()
        {
            yield return new object[]
            {
                new Address { State = "DC" },
                "{ \"replaceWith\": null }",
                "{}"
            };
            yield return new object[] 
            {
                new Address { State = "DC" },
                "{ \"replaceWith\": {\r\n  \"use\": \"home\",\r\n  \"type\": \"both\",\r\n  \"text\": \"room\",\r\n  \"city\": \"Beijing\",\r\n  \"district\": \"Haidian\",\r\n  \"state\": \"Beijing\",\r\n  \"postalCode\": \"100871\",\r\n  \"period\": {\r\n    \"start\": \"1974-12-25\"\r\n  }\r\n} }",
                "{\r\n  \"state\": \"Beijing\",\r\n  \"use\": \"home\",\r\n  \"type\": \"both\",\r\n  \"text\": \"room\",\r\n  \"city\": \"Beijing\",\r\n  \"district\": \"Haidian\",\r\n  \"postalCode\": \"100871\",\r\n  \"period\": {\r\n    \"start\": \"1974-12-25\"\r\n  }\r\n}"
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }  },
                "{ \"replaceWith\": {\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\"\r\n  ]\r\n} }",
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\"\r\n  ]\r\n}"
            };
            yield return new object[]
            {
                new ContactPoint { Use = ContactPoint.ContactPointUse.Home, System = ContactPoint.ContactPointSystem.Email, Value = "test@example.com", Period = new Period { Start = "2018-01-01", End = "2019-12-30" } } ,
                "{ \"replaceWith\": {\r\n  \"use\": \"work\",\r\n  \"system\": \"phone\",\r\n  \"value\": \"12345678\",\r\n  \"period\": {\r\n  \"start\": \"2020\",\r\n\"end\": \"\"  }\r\n}\r\n}",
                "{\r\n  \"system\": \"phone\",\r\n  \"value\": \"12345678\",\r\n  \"use\": \"work\",\r\n  \"period\": {\r\n    \"start\": \"2020\"\r\n  }\r\n}"
            };
        }

        public static IEnumerable<object[]> GetInvalidReplaceValue()
        {
            yield return new object[]
            {
                new Address { State = "DC" },
                "{\"replaceWith\": \"\"}"
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }  },
                "{\"replaceWith\": \"2019-01-01\"}"
            };
            yield return new object[]
            {
                new ContactPoint { Use = ContactPoint.ContactPointUse.Home, System = ContactPoint.ContactPointSystem.Email, Value = "test@example.com", Period = new Period { Start = "2018-01-01", End = "2019-12-30" } } ,
                "{\"replaceWith\": {\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\"\r\n  ]\r\n} }",
            };
        }

        public static IEnumerable<object[]> GetConflictRuleNodes()
        {
            yield return new object[]
            {
                new Id("123"),
                "$this",
                "{\"replaceWith\": \"abv\"}",
                "123",
                true
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }  },
                "HumanName.given[0]",
                "{\"replaceWith\": {\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\",\r\n    \"Cook\"\r\n  ]\r\n} }",
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Chris\",\r\n    \"Cook\"\r\n  ]\r\n}"
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }, Period = new Period { Start = "2020-01-01T00:00:00.000Z", End = "2022-01-01T14:01:01.000Z"}  },
                "HumanName.period.start",
                "{\"replaceWith\":{}}",
                "{\r\n  \"period\": {\r\n    \"start\": \"2020-01-01T00:00:00.000Z\"\r\n  }\r\n}"
            };
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveNodes))]
        public void GivenAPrimitiveNode_WhenSubstitute_SubstitutedNodeShouldBeReturned(Base data, string configJson)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            SubstituteProcessor processor = new SubstituteProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ITypedElement>()
            };
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsSubstituted);
            Assert.Equal(node.Value?.ToString(), settings["replaceWith"]?.ToString());
        }

        [Theory]
        [MemberData(nameof(GetComplexNodes))]
        public void GivenAComplexDatatypeNodeAndValidReplaceValue_WhenSubstitute_SubstituteNodeShouldBeReturned(Base data, string configJson, string targetJson)
        {
            targetJson = targetJson.Replace("\r\n", Environment.NewLine);

            SubstituteProcessor processor = new SubstituteProcessor();
            var node = ElementNode.FromElement(data.ToTypedElement());
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ITypedElement>()
            };
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsSubstituted);
            Assert.Equal(targetJson, Standardize(node));
        }

        [Theory]
        [MemberData(nameof(GetConflictRuleNodes))]
        public void GivenANodeWithConflictRuleProcessedChild_WhenSubstitute_PreviousResultShouldBeKept(Base data, string processedNodePath, string configJson, string targetJson, bool isPrimitive = false)
        {
            targetJson = targetJson.Replace("\r\n", Environment.NewLine);

            SubstituteProcessor processor = new SubstituteProcessor();
            var node = ElementNode.FromElement(data.ToTypedElement());
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ITypedElement>(node.Select(processedNodePath).CastElementNodes())
            }; 
            Assert.NotEmpty(context.VisitedNodes);
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);

            var processResult = processor.Process(node, context, settings);
            if (isPrimitive)
            {
                // Primitive types are not substituted
                Assert.False(processResult.IsSubstituted);
                Assert.Equal(targetJson, node.Value);
            }
            else
            {
                Assert.True(processResult.IsSubstituted);
                Assert.Equal(targetJson, Standardize(node));
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidReplaceValue))]
        public void GivenAComplexDatatypeNodeAndInvalidReplaceValue_WhenSubstitute_FormatExceptionShouldBeThrown(Base data, string configJson)
        {
            SubstituteProcessor processor = new SubstituteProcessor();
            var node = ElementNode.FromElement(data.ToTypedElement());
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ITypedElement>()
            };
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);

            Assert.Throws<FormatException>(() => processor.Process(node, context, settings));
        }

        private static string Standardize(ElementNode node)
        {
            FhirJsonSerializationSettings serializationSettings = new FhirJsonSerializationSettings
            {
                Pretty = true,
            };
            
            return node.ToJson(serializationSettings);
        }
    }
}
