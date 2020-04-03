using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Newtonsoft.Json;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.ResourceTransformers
{
    public class ResourceIdTransformerTests
    {
        private ResourceIdTransformer _idTransformer = new ResourceIdTransformer();
        public ResourceIdTransformerTests()
        {
            var _presetIdMappings = new Dictionary<string, string>
            {
                { "example", "example-abc" },
                { "p1", "p1-abc" },
                { "c8973a22-2b5b-4e76-9c66-00639c99e61b", "074d000b-1917-40da-a572-6d2b46c37c21" },
                { "c757873d-ec9a-4326-a141-556f43239520", "d05fc8cb-5f9d-4852-8629-d5205a8a5b28" },
                { "1.2.3.4.5", "069e1601-005a-4886-b5cd-864cc5bf12e1" }
            };
            _idTransformer.LoadExistingMapping(_presetIdMappings);

            _testConditionNode = ElementNode.
                FromElement(new FhirJsonParser().Parse(TestContitionSample).ToTypedElement());
        }

        public static IEnumerable<object[]> GetLiteralReferenceData()
        {
            yield return new string[] { "Patient/example", "Patient/example-abc" };
            yield return new string[] { "#p1", "#p1-abc" };
            yield return new string[] { "http://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b", "http://fhir.hl7.org/svc/StructureDefinition/074d000b-1917-40da-a572-6d2b46c37c21" };
            yield return new string[] { "urn:uuid:c757873d-ec9a-4326-a141-556f43239520", "urn:uuid:d05fc8cb-5f9d-4852-8629-d5205a8a5b28" };
            yield return new string[] { "urn:oid:1.2.3.4.5", "urn:uuid:069e1601-005a-4886-b5cd-864cc5bf12e1" };
            yield return new string[] { "#", "#" }; // Internal reference to container
        }

        public static IEnumerable<object[]> GetResourceIdData()
        {
            yield return new string[] { "example", "example-abc" };
            yield return new string[] { "p1", "p1-abc" };
            yield return new string[] { "c8973a22-2b5b-4e76-9c66-00639c99e61b", "074d000b-1917-40da-a572-6d2b46c37c21" };
            yield return new string[] { "c757873d-ec9a-4326-a141-556f43239520", "d05fc8cb-5f9d-4852-8629-d5205a8a5b28" };
            yield return new string[] { "1.2.3.4.5", "069e1601-005a-4886-b5cd-864cc5bf12e1" };
        }

        [Fact]
        public void GivenAElementNode_WhenTransformingId_ResourceIdsShouldBeTransformed()
        {
            _idTransformer.Transform(_testConditionNode);
            Assert.Equal("example-abc", _testConditionNode.Select("Condition.id").First().Value.ToString());
            Assert.Equal("#p1-abc", _testConditionNode.Select("Condition.subject.reference").First().Value.ToString());
        }

        [Fact]
        public void GivenAMappingFileName_WhenSave_MappingDataShouldBeSaved()
        {
            var mappingFileName = "testmapping.txt";
            _idTransformer.SaveMappingFile(mappingFileName);
            Assert.True(File.Exists(mappingFileName));

            var idMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(mappingFileName));
            Assert.True(idMap.ContainsKey("p1"));
            Assert.False(idMap.ContainsKey("test"));
            Assert.Equal("example-abc", idMap["example"]);
        }

        [Theory]
        [MemberData(nameof(GetLiteralReferenceData))]
        public void GivenALiteralReference_WhenTransforming_CorrectIdShouldBeReplaced(string reference, string expectedReferece)
        {
            var result = _idTransformer.TransformIdFromReference(reference);
            Assert.Equal(expectedReferece, result);
        }

        [Theory]
        [MemberData(nameof(GetResourceIdData))]
        public void GivenAResourceId_WhenTransforming_CorrectNewIdShouldBeReturned(string id, string expectedId)
        {
            var result = _idTransformer.TransformId(id);
            Assert.Equal(expectedId, result);
        }

        private ElementNode _testConditionNode;

        private const string TestContitionSample =
@"{
  ""resourceType"" : ""Condition"",
  ""id"":""example"",
  ""contained"": [
    {
      ""resourceType"" : ""Practitioner"",
      ""id"" : ""p1"",
      ""name"" : [{
        ""family"" : ""Person"",
        ""given"" : [""Patricia""]
    }]
	  }],
   ""subject"" : {
     ""reference"" : ""#p1""
  }
}";
    }
}
