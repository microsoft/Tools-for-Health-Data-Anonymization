using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTest.Processors
{
    public class SubstituteProcessorTests
    {
        [Fact]
        public void GivenAGeneratedDatatypeNodeAndValidReplaceValue_WhenSubstitute_SubstituteNodeShouldBeReturned()
        {
            SubstituteProcessor processor = new SubstituteProcessor();
            Address testaddress = new Address() { State = "DC" };
            var node = ElementNode.FromElement(testaddress.ToTypedElement());
            var processSetting = new ProcessSetting
            {
                ReplaceWith = "{\r\n  \"use\": \"home\",\r\n  \"type\": \"both\",\r\n  \"text\": \"room\",\r\n  \"city\": \"Beijing\",\r\n  \"district\": \"Haidian\",\r\n  \"state\": \"Beijing\",\r\n  \"postalCode\": \"100871\",\r\n  \"period\": {\r\n    \"start\": \"1974-12-25\"\r\n  }\r\n}",
                IsPrimitiveReplacement = false,
                VisitedNodes = new HashSet<ElementNode>()
            };

            var processResult = processor.Process(node, processSetting);
            Assert.Equal("{\r\n  \"state\": \"Beijing\",\r\n  \"use\": \"home\",\r\n  \"type\": \"both\",\r\n  \"text\": \"room\",\r\n  \"city\": \"Beijing\",\r\n  \"district\": \"Haidian\",\r\n  \"postalCode\": \"100871\",\r\n  \"period\": {\r\n    \"start\": \"1974-12-25\"\r\n  }\r\n}", Standardize(node));
            Assert.True(processResult.IsAbstracted);
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
