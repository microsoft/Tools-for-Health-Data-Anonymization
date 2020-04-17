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
        private ElementNode _testConditionNode;
        private readonly ResourceIdTransformer _idTransformer = new ResourceIdTransformer();
        public ResourceIdTransformerTests()
        {
            _testConditionNode = ElementNode.
                FromElement(new FhirJsonParser().Parse(TestContitionSample).ToTypedElement());
        }

        public static IEnumerable<object[]> GetLiteralReferenceData()
        {
            yield return new string[] { "Patient/example", "Patient/50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c" };
            yield return new string[] { "#", "#" }; // Internal reference to container
            yield return new string[] { "#p1", "#f64551fcd6f07823cb87971cfb91446425da18286b3ab1ef935e0cbd7a69f68a" };
            yield return new string[] { "http://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b", "http://fhir.hl7.org/svc/StructureDefinition/fc078fc8fcc2ee52c2b151f722613cd3c4bdf28e9b17d082630d3c206c363613" };
            yield return new string[] { "http://example.org/fhir/Observation/apo89654/_history/2", "http://example.org/fhir/Observation/c6f2a060732f93e17aa7d213dde046eda81be66157babac462bdfe0d6054764c/_history/2" };
            yield return new string[] { "urn:uuid:c757873d-ec9a-4326-a141-556f43239520", "urn:uuid:1b390ce369c88527d1cc8bc1b5091fe82fde53c063f03df23531ca02b99ce5af" };
            yield return new string[] { "urn:oid:1.2.3.4.5", "urn:oid:b8b9f26c0d51e56b892f8ecdc61968867eb1e1da1bcdcc05f8b7d2597147a2e2" };
        }

        public static IEnumerable<object[]> GetResourceIdData()
        {
            yield return new string[] { "example", "50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c" };
            yield return new string[] { "p1", "f64551fcd6f07823cb87971cfb91446425da18286b3ab1ef935e0cbd7a69f68a" };
            yield return new string[] { "c8973a22-2b5b-4e76-9c66-00639c99e61b", "fc078fc8fcc2ee52c2b151f722613cd3c4bdf28e9b17d082630d3c206c363613" };
            yield return new string[] { "c757873d-ec9a-4326-a141-556f43239520", "1b390ce369c88527d1cc8bc1b5091fe82fde53c063f03df23531ca02b99ce5af" };
            yield return new string[] { "1.2.3.4.5", "b8b9f26c0d51e56b892f8ecdc61968867eb1e1da1bcdcc05f8b7d2597147a2e2" };
        }

        [Fact]
        public void GivenAElementNode_WhenTransformingId_ResourceIdsShouldBeTransformed()
        {
            _idTransformer.Transform(_testConditionNode);
            Assert.Equal("50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c", _testConditionNode.Select("Condition.id").First().Value.ToString());
            Assert.Equal("#f64551fcd6f07823cb87971cfb91446425da18286b3ab1ef935e0cbd7a69f68a", _testConditionNode.Select("Condition.subject.reference").First().Value.ToString());
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
