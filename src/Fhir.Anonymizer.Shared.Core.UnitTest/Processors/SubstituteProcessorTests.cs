using System.Collections.Generic;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;
using System;
using Hl7.FhirPath;
using System.Linq;
using System.Diagnostics;
using System.Dynamic;

namespace Fhir.Anonymizer.Core.UnitTest.Processors
{
    public class SubstituteProcessorTests
    {
        public static IEnumerable<object[]> GetPrimitiveNodes()
        {
            yield return new object[] { new FhirBoolean(true), null };
            yield return new object[] { new FhirBoolean(true), string.Empty };
            yield return new object[] { new FhirBoolean(true), "false" };
            yield return new object[] { new Id("123"), "abc" };
            yield return new object[] { new FhirDecimal(1), "1" };
            yield return new object[] { new Date("2000"), "2020" };
        }

        public static IEnumerable<object[]> GetComplexNodes()
        {
            yield return new object[] 
            {
                new Address { State = "DC" },
                "{\r\n  \"use\": \"home\",\r\n  \"type\": \"both\",\r\n  \"text\": \"room\",\r\n  \"city\": \"Beijing\",\r\n  \"district\": \"Haidian\",\r\n  \"state\": \"Beijing\",\r\n  \"postalCode\": \"100871\",\r\n  \"period\": {\r\n    \"start\": \"1974-12-25\"\r\n  }\r\n}",
                "{\r\n  \"state\": \"Beijing\",\r\n  \"use\": \"home\",\r\n  \"type\": \"both\",\r\n  \"text\": \"room\",\r\n  \"city\": \"Beijing\",\r\n  \"district\": \"Haidian\",\r\n  \"postalCode\": \"100871\",\r\n  \"period\": {\r\n    \"start\": \"1974-12-25\"\r\n  }\r\n}"
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }  },
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\"\r\n  ]\r\n}",
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\"\r\n  ]\r\n}"
            };
            yield return new object[]
            {
                new ContactPoint { Use = ContactPoint.ContactPointUse.Home, System = ContactPoint.ContactPointSystem.Email, Value = "test@example.com", Period = new Period { Start = "2018-01-01", End = "2019-12-30" } } ,
                "{\r\n  \"use\": \"work\",\r\n  \"system\": \"phone\",\r\n  \"value\": \"12345678\",\r\n  \"period\": {\r\n  \"start\": \"2020\",\r\n\"end\": \"\"  }\r\n}\r\n}",
                "{\r\n  \"system\": \"phone\",\r\n  \"value\": \"12345678\",\r\n  \"use\": \"work\",\r\n  \"period\": {\r\n    \"start\": \"2020\"\r\n  }\r\n}"
            };
        }

        public static IEnumerable<object[]> GetInvalidReplaceValue()
        {
            yield return new object[]
            {
                new Address { State = "DC" },
                null
            };
            yield return new object[]
            {
                new Address { State = "DC" },
                string.Empty
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }  },
                "2019-01-01"
            };
            yield return new object[]
            {
                new ContactPoint { Use = ContactPoint.ContactPointUse.Home, System = ContactPoint.ContactPointSystem.Email, Value = "test@example.com", Period = new Period { Start = "2018-01-01", End = "2019-12-30" } } ,
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\"\r\n  ]\r\n}",
            };
        }

        public static IEnumerable<object[]> GetConflictRuleNodes()
        {
            yield return new object[]
            {
                new Id("123"),
                "$this",
                "abv",
                "123",
                true
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }  },
                "HumanName.given[0]",
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Jim\",\r\n    \"Cook\"\r\n  ]\r\n}",
                "{\r\n  \"use\": \"nickname\",\r\n  \"family\": \"Harry\",\r\n  \"given\": [\r\n    \"Chris\",\r\n    \"Cook\"\r\n  ]\r\n}"
            };
            yield return new object[]
            {
                new HumanName { Use = HumanName.NameUse.Official, Family = "Green", Given = new List<string> { "Chris", "Jason" }, Period = new Period { Start = "2020-01-01T00:00:00.000Z", End = "2022-01-01T14:01:01.000Z"}  },
                "HumanName.period.start",
                "{}",
                "{\r\n  \"period\": {\r\n    \"start\": \"2020-01-01T00:00:00.000Z\"\r\n  }\r\n}"
            };
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveNodes))]
        public void GivenAPrimitiveNode_WhenSubstitute_SubstitutedNodeShouldBeReturned(Base data, string replaceWith)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            SubstituteProcessor processor = new SubstituteProcessor();
            var processResult = processor.Process(node, new ProcessSetting
            {
                ReplaceWith = replaceWith,
                IsPrimitiveReplacement = true,
                VisitedNodes = new HashSet<ElementNode>()
            });

            Assert.True(processResult.IsAbstracted);
            Assert.Equal(node.Value, replaceWith ?? string.Empty);
        }

        [Theory]
        [MemberData(nameof(GetComplexNodes))]
        public void GivenAComplexDatatypeNodeAndValidReplaceValue_WhenSubstitute_SubstituteNodeShouldBeReturned(Base data, string replaceWith, string targetJson)
        {
            SubstituteProcessor processor = new SubstituteProcessor();
            var node = ElementNode.FromElement(data.ToTypedElement());
            var processSetting = new ProcessSetting
            {
                ReplaceWith = replaceWith,
                IsPrimitiveReplacement = false,
                VisitedNodes = new HashSet<ElementNode>()
            };

            var processResult = processor.Process(node, processSetting);
            Assert.True(processResult.IsAbstracted);
            Assert.Equal(targetJson, Standardize(node));
        }

        [Theory]
        [MemberData(nameof(GetConflictRuleNodes))]
        public void GivenANodeWithConflictRuleProcessedChild_WhenSubstitute_PreviousResultShouldBeKept(Base data, string processedNodePath, string replaceWith, string targetJson, bool isPrimitive = false)
        {
            SubstituteProcessor processor = new SubstituteProcessor();
            var node = ElementNode.FromElement(data.ToTypedElement());
            var processSetting = new ProcessSetting
            {
                ReplaceWith = replaceWith,
                IsPrimitiveReplacement = isPrimitive,
                VisitedNodes = node.Select(processedNodePath).Cast<ElementNode>().ToHashSet()
            };
            Assert.NotEmpty(processSetting.VisitedNodes);

            var processResult = processor.Process(node, processSetting);
            if (isPrimitive)
            {
                // Primitive types are not substuted
                Assert.False(processResult.IsAbstracted);
                Assert.Equal(targetJson, node.Value);
            }
            else
            {
                Assert.True(processResult.IsAbstracted);
                Assert.Equal(targetJson, Standardize(node));
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidReplaceValue))]
        public void GivenAComplexDatatypeNodeAndInvalidReplaceValue_WhenSubstitute_FormatExceptionShouldBeThrown(Base data, string replaceWith)
        {
            SubstituteProcessor processor = new SubstituteProcessor();
            var node = ElementNode.FromElement(data.ToTypedElement());
            var processSetting = new ProcessSetting
            {
                ReplaceWith = replaceWith,
                IsPrimitiveReplacement = false,
                VisitedNodes = new HashSet<ElementNode>()
            };

            Assert.Throws<FormatException>(() => processor.Process(node, processSetting));
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
