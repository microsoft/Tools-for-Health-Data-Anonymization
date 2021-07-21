using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class CryptoHashProcessorTests
    {
        private const string TestHashKey = "123";

        public static IEnumerable<object[]> GetNonReferenceNodesForCryptoHash()
        {
            yield return new object[] { new Id(string.Empty), string.Empty };
            yield return new object[] { new Id("a"), "69e99e82127c1f146f50653e02b92c4bb0c3bc182a6165a5bbce5f4f94e1ccb7" };
            yield return new object[] { new Id("bb6f4872-e456-42d5-a9da-a0d82cb7ea29"), "73defcb8fcaf4c0c3d5c77f05b479cadbe502db2ef6e9b1523d2bfee31f3b999" };
            yield return new object[] { new Oid("urn:oid:1.2.3.4.5"), "6aa8e3e9af18cae990adc9c26dc006ebf633dd1332886867a691ee2d5247dd15" };
            yield return new object[] { new Uuid("urn:uuid:c757873d-ec9a-4326-a141-556f43239520"), "23a940be8f03522b52a393c7194407796bb5ea3c02b926b0e42fdab94ca30bad" };
            yield return new object[] { new Date("2020-04-12"), "1e99c6a8b99d3c8b4906c2e80911ad3b5961fd7498d3bc2b96fb128bc7148f90" };
            yield return new object[] { new FhirDateTime("2017-01-01T00:00:00.000Z"), "4c765fc04a6f9967d493ff39238d47993c709d3392a72060efeff285cf7b2501" };
        }

        public static IEnumerable<object[]> GetReferenceNodesForCryptoHash()
        {
            yield return new object[] { new ResourceReference(string.Empty), string.Empty };
            yield return new object[] { new ResourceReference("#"), "#" };
            yield return new object[] { new ResourceReference("#p1"), "#a10e6bee4fbeb6a7804153c25688dd4dd7b9c2a005417136026350fc33ac609f" };
            yield return new object[] { new ResourceReference("Patient/example"), "Patient/698d54f0494528a759f19c8e87a9f99e75a5881b9267ee3926bcf62c992d84ba" };
            yield return new object[] { new ResourceReference("http://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b"),
                "http://fhir.hl7.org/svc/StructureDefinition/b0ff9c939b3507a79e2ae3d2d3b595d62819b9b8f6ef10d4099b3058d902642f" };
            yield return new object[] { new ResourceReference("http://example.org/fhir/Observation/apo89654/_history/2"),
                "http://example.org/fhir/Observation/b1e85ca33baf76575ad28588af85b8f10c0dd40e9ed8cd57cdb7ae94ccd75695/_history/2" };
            yield return new object[] { new ResourceReference("urn:uuid:c757873d-ec9a-4326-a141-556f43239520"),
                "urn:uuid:24970eb3f915e516a2b5241c0d6979097a6357a13b89612c6a54b8ab5479df34" };
            yield return new object[] { new ResourceReference("urn:oid:1.2.3.4.5"),
                "urn:oid:0543fb50485f58a47073f51aad1677607aec031c2c83c25ee7b040ade95cfbcc" };
        }

        [Theory]
        [MemberData(nameof(GetNonReferenceNodesForCryptoHash))]
        public void GivenANonReferenceNode_WhenCryptoHash_HashedNodeShouldBeReturned(Element element, string expectedValue)
        {
            var processor = new CryptoHashProcessor(TestHashKey);
            var node = CreateNodeFromElement(element);
            processor.Process(node);
            Assert.Equal(expectedValue, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetReferenceNodesForCryptoHash))]
        public void GivenAReferenceNode_WhenCryptoHash_PartlyHashedNodeShouldBeReturned(ResourceReference reference, string expectedValue)
        {
            var processor = new CryptoHashProcessor(TestHashKey);

            // If an ElementNode is created by ElementNode.FromElement(), its children are of type ElementNode
            // Cast them to ElementNode directly
            // https://github.com/FirelyTeam/firely-net-common/blob/master/src/Hl7.Fhir.ElementModel/ElementNode.cs
            var referenceNode = CreateNodeFromElement(reference).Children("reference").Cast<ElementNode>().FirstOrDefault();
            processor.Process(referenceNode);
            Assert.Equal(expectedValue, referenceNode.Value);
        }

        private static ElementNode CreateNodeFromElement(Element element)
        {
            return ElementNode.FromElement(element.ToTypedElement());
        }
    }
}
