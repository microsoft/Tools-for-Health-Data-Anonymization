using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Extensions
{
    public class TypedElementNavExtensionsTests
    {
        [Fact]
        public void GivenASingleResourceNode_WhenGetResourceDescendantsWithoutSubResource_DescendantsShouldBeReturned()
        {
            var patient = new Patient();
            patient.Address.Add(new Address());
            patient.Name.Add(new HumanName { Given = new[] { "Test" } });

            var testNode = patient.ToTypedElement();
            var result = testNode.ResourceDescendantsWithoutSubResource().Select(e => e.Location).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains("Patient.name[0].given[0]", result);
            Assert.Contains("Patient.name[0]", result);
            Assert.Contains("Patient.address[0]", result);
        }

        [Fact]
        public void GivenAContainedNode_WhenGetResourceDescendantsWithoutSubResource_ContainedNodesShouldNotBeReturned()
        {
            var patient = new Patient();
            patient.Address.Add(new Address());
            patient.Name.Add(new HumanName { Given = new[] { "Test" } });

            var condition = new Condition();
            condition.Text = new Narrative { Div = "Test" };
            condition.Contained.Add(patient);
            
            var testNode = condition.ToTypedElement();
            var result = testNode.ResourceDescendantsWithoutSubResource().Select(e => e.Location).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains("Condition.text[0]", result);
            Assert.Contains("Condition.text[0].div[0]", result);
        }

        [Fact]
        public void GivenASingleResourceNode_WhenGetSelfAndDescendantsWithoutSubResource_SelfAndDescendantsShouldBeReturned()
        {
            var patient = new Patient();
            patient.Address.Add(new Address());
            patient.Name.Add(new HumanName { Given = new[] { "Test" } });

            var testNodes = new List<ITypedElement> {patient.ToTypedElement()};
            var result = testNodes.SelfAndDescendantsWithoutSubResource().Select(e => e.Location).ToList();

            Assert.Equal(4, result.Count);
            Assert.Contains("Patient", result);
            Assert.Contains("Patient.name[0].given[0]", result);
            Assert.Contains("Patient.name[0]", result);
            Assert.Contains("Patient.address[0]", result);
        }

        [Fact]
        public void GivenAContainedNode_WhenSelfAndDescendantsWithoutSubResource_ContainedNodesShouldNotBeReturned()
        {
            var patient = new Patient();
            patient.Address.Add(new Address());
            patient.Name.Add(new HumanName { Given = new[] { "Test" } });

            var condition = new Condition();
            condition.Text = new Narrative { Div = "Test" };
            condition.Contained.Add(patient);

            var testNodes = new List<ITypedElement> { condition.ToTypedElement() };
            var result = testNodes.SelfAndDescendantsWithoutSubResource().Select(e => e.Location).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains("Condition", result);
            Assert.Contains("Condition.text[0]", result);
            Assert.Contains("Condition.text[0].div[0]", result);
        }
    }
}
