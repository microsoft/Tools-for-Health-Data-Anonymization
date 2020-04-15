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
