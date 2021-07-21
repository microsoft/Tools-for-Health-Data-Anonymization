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
            var (root, child1, child2) = GetSampleNodes();
            Assert.Equal(2, root.Children().Count());

            root.RemoveNullChildren();
            Assert.Equal(2, root.Children().Count());

            child1.Value = null;
            root.RemoveNullChildren();
            Assert.Single(root.Children());

            child2.Value = null;
            root.RemoveNullChildren();
            Assert.Empty(root.Children());
        }

        private (ElementNode, ElementNode, ElementNode) GetSampleNodes()
        {
            var root = ElementNode.FromElement(new FhirString("root").ToTypedElement());
            var child1 = ElementNode.FromElement(new FhirString("child1").ToTypedElement());
            var child2 = ElementNode.FromElement(new FhirString("child2").ToTypedElement());
            root.Name = "root";
            child1.Name = "child1";
            child2.Name = "child2";
            root.Add(_provider, child1);
            root.Add(_provider, child2);

            return (root, child1, child2);
        }
    }
}
