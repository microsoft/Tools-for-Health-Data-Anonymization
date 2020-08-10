using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Extensions
{
    public class ElementNodeExtensionTests
    {
        private readonly PocoStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();

        [Fact]
        public void GivenAnElementNode_WhenRemoveNullChildren_NullChildrenShouldBeRemoved()
        {
            var node = GetSampleNode();
            Assert.Equal(2, node.Children().Count());

            node.RemoveNullChildren();
            Assert.Equal(2, node.Children().Count());

            node.Children("child1").Cast<ElementNode>().First().Value = null;
            node.RemoveNullChildren();
            Assert.Single(node.Children());

            node.Children("child2").Cast<ElementNode>().First().Value = null;
            node.RemoveNullChildren();
            Assert.Empty(node.Children());
        }

        private ElementNode GetSampleNode()
        {
            var root = ElementNode.FromElement(new FhirString("root").ToTypedElement());
            var child1 = ElementNode.FromElement(new FhirString("child1").ToTypedElement());
            var child2 = ElementNode.FromElement(new FhirString("child2").ToTypedElement());
            root.Name = "root";
            child1.Name = "child1";
            child2.Name = "child2";
            root.Add(_provider, child1);
            root.Add(_provider, child2);

            return root;
        }
    }
}
