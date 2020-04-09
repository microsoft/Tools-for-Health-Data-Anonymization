using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.ExtensionTests
{
    public class ElementNodeNavExtensionTests
    {
        [Fact]
        public void GivenAnBundleNode_WhenGetSubResourceNodesAndSelf_ResourceEntryNodesShouldBeReturned()
        {
            Bundle bundle = new Bundle();
            bundle.AddResourceEntry(new Patient(), "http://example.org/fhir/Patient/1");
            bundle.AddResourceEntry(new Person(), "http://example.org/fhir/Person/1");

            var testNodes = ElementNode.FromElement(bundle.ToTypedElement());
            var result = testNodes.SubResourceNodesAndSelf().ToList();

            Assert.Equal(3, result.Count());
            Assert.Equal("Bundle", result[0].InstanceType);
            Assert.Equal("Patient", result[1].InstanceType);
            Assert.Equal("Person", result[2].InstanceType);
        }

        [Fact]
        public void GivenAnContainedNode_WhenGetSubResourceNodesAndSelf_ResourceNodesShouldBeReturned()
        {
            Condition condition = new Condition();
            condition.Contained.Add(new Patient());
            condition.Contained.Add(new Person());

            var testNodes = ElementNode.FromElement(condition.ToTypedElement());
            var result = testNodes.SubResourceNodesAndSelf().ToList();

            Assert.Equal(3, result.Count());
            Assert.Equal("Condition", result[0].InstanceType);
            Assert.Equal("Patient", result[1].InstanceType);
            Assert.Equal("Person", result[2].InstanceType);
        }

        [Fact]
        public void GivenAnSingleResourceNode_WhenGetSubResourceNodesAndSelf_NodeSelfShouldBeReturned()
        {
            Patient patient = new Patient();

            var testNodes = ElementNode.FromElement(patient.ToTypedElement());
            var result = testNodes.SubResourceNodesAndSelf().ToList();

            Assert.Single(result);
            Assert.Equal("Patient", result[0].InstanceType);
        }

        [Fact]
        public void GivenAnSingleResourceNode_WhenGetResourceDescendants_DescendantsShouldBeReturned()
        {
            Patient patient = new Patient();
            patient.Address.Add(new Address());
            patient.Name.Add(new HumanName() { Given = new string[] { "Test" } });

            var testNodes = ElementNode.FromElement(patient.ToTypedElement());
            var result = testNodes.ResourceDescendants().Select(e => e.Location).ToList();

            Assert.Equal(3, result.Count());
            Assert.Contains("Patient.name[0].given[0]", result);
            Assert.Contains("Patient.name[0]", result);
            Assert.Contains("Patient.address[0]", result);
        }

        [Fact]
        public void GivenAnContianedNode_WhenGetResourceDescendants_ContainedNodesShouldNotBeReturned()
        {
            Patient patient = new Patient();
            patient.Address.Add(new Address());
            patient.Name.Add(new HumanName() { Given = new string[] { "Test" } });

            Condition condition = new Condition();
            condition.Text = new Narrative() { Div = "Test" };
            condition.Contained.Add(patient);
            
            var testNodes = ElementNode.FromElement(condition.ToTypedElement());
            var result = testNodes.ResourceDescendants().Select(e => e.Location).ToList();

            Assert.Equal(2, result.Count());
            Assert.Contains("Condition.text[0]", result);
            Assert.Contains("Condition.text[0].div[0]", result);
        }
    }
}
