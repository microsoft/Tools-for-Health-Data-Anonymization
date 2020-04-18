using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.ResourceTransformers
{
    public class ResourceIdTransformerTests
    {
        private ElementNode _testConditionNode;
        private readonly ResourceIdTransformer _idTransformer = new ResourceIdTransformer("123");
        public ResourceIdTransformerTests()
        {
            _testConditionNode = ElementNode.
                FromElement(new FhirJsonParser().Parse(TestContitionSample).ToTypedElement());
        }

        public static IEnumerable<object[]> GetLiteralReferenceData()
        {
            yield return new string[] { "Patient/example", "Patient/698d54f0494528a759f19c8e87a9f99e75a5881b9267ee3926bcf62c992d84ba" };
            yield return new string[] { "#", "#" }; // Internal reference to container
            yield return new string[] { "#p1", "#a10e6bee4fbeb6a7804153c25688dd4dd7b9c2a005417136026350fc33ac609f" };
            yield return new string[] { "http://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b", "http://fhir.hl7.org/svc/StructureDefinition/b0ff9c939b3507a79e2ae3d2d3b595d62819b9b8f6ef10d4099b3058d902642f" };
            yield return new string[] { "http://example.org/fhir/Observation/apo89654/_history/2", "http://example.org/fhir/Observation/b1e85ca33baf76575ad28588af85b8f10c0dd40e9ed8cd57cdb7ae94ccd75695/_history/2" };
            yield return new string[] { "urn:uuid:c757873d-ec9a-4326-a141-556f43239520", "urn:uuid:24970eb3f915e516a2b5241c0d6979097a6357a13b89612c6a54b8ab5479df34" };
            yield return new string[] { "urn:oid:1.2.3.4.5", "urn:oid:0543fb50485f58a47073f51aad1677607aec031c2c83c25ee7b040ade95cfbcc" };
        }

        public static IEnumerable<object[]> GetResourceIdData()
        {
            yield return new string[] { "example", "698d54f0494528a759f19c8e87a9f99e75a5881b9267ee3926bcf62c992d84ba" };
            yield return new string[] { "p1", "a10e6bee4fbeb6a7804153c25688dd4dd7b9c2a005417136026350fc33ac609f" };
            yield return new string[] { "c8973a22-2b5b-4e76-9c66-00639c99e61b", "b0ff9c939b3507a79e2ae3d2d3b595d62819b9b8f6ef10d4099b3058d902642f" };
            yield return new string[] { "c757873d-ec9a-4326-a141-556f43239520", "24970eb3f915e516a2b5241c0d6979097a6357a13b89612c6a54b8ab5479df34" };
            yield return new string[] { "1.2.3.4.5", "0543fb50485f58a47073f51aad1677607aec031c2c83c25ee7b040ade95cfbcc" };
        }

        [Fact]
        public void GivenAElementNode_WhenTransformingId_ResourceIdsShouldBeTransformed()
        {
            _idTransformer.Transform(_testConditionNode);
            Assert.Equal("698d54f0494528a759f19c8e87a9f99e75a5881b9267ee3926bcf62c992d84ba", _testConditionNode.Select("Condition.id").First().Value.ToString());
            Assert.Equal("#a10e6bee4fbeb6a7804153c25688dd4dd7b9c2a005417136026350fc33ac609f", _testConditionNode.Select("Condition.subject.reference").First().Value.ToString());
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
            var result = _idTransformer.TransformResourceId(id);
            Assert.Equal(expectedId, result);
        }

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
