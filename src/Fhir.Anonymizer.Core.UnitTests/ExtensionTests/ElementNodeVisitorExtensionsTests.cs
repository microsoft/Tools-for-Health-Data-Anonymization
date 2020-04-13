using System;
using System.Collections.Generic;
using System.Text;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.ExtensionTests
{
    public class ElementNodeVisitorExtensionsTests
    {
        [Fact]
        public void GivenAPatientNode_WhenVisit_AllNodesShouldBeVisited()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test" });
            patient.Address.Add(new Address());

            var node = ElementNode.FromElement(patient.ToTypedElement());
            var context = new HashSet<string>();
            node.Accept(new TestVisitor(), context);

            Assert.Equal(5, context.Count);
            Assert.Contains("Patient", context);
            Assert.Contains("Patient.active[0]", context);
            Assert.Contains("Patient.address[0]", context);
            Assert.Contains("Patient.address[0].city[0]", context);
            Assert.Contains("Patient.address[1]", context);
        }

        [Fact]
        public void GivenAPatientNodeWithContained_WhenVisit_ContainedNodesShouldNotBeVisited()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test" });
            patient.Address.Add(new Address());
            patient.Contained.Add(new Observation() {  Status = ObservationStatus.Unknown });

            var node = ElementNode.FromElement(patient.ToTypedElement());
            var context = new HashSet<string>();
            node.Accept(new TestVisitor(), context);

            Assert.Equal(5, context.Count);
            Assert.Contains("Patient", context);
            Assert.Contains("Patient.active[0]", context);
            Assert.Contains("Patient.address[0]", context);
            Assert.Contains("Patient.address[0].city[0]", context);
            Assert.Contains("Patient.address[1]", context);
        }

        [Fact]
        public void GivenABundleNode_WhenVisit_BundleEntryNodesShouldNotBeVisited()
        {
            Bundle bundle = new Bundle();
            bundle.Timestamp = new DateTimeOffset();
            
            Patient patient = new Patient();
            patient.Active = true;

            bundle.AddResourceEntry(patient, "http://example.org/fhir/Patient/1");

            var node = ElementNode.FromElement(bundle.ToTypedElement());
            var context = new HashSet<string>();
            node.Accept(new TestVisitor(), context);

            Assert.Equal(2, context.Count);
            Assert.Contains("Bundle", context);
            Assert.Contains("Bundle.timestamp[0]", context);
        }

        private class TestVisitor : AbstractElementNodeVisitor<HashSet<string>>
        {
            public override bool Visit(ElementNode node, HashSet<string> context)
            {
                context.Add(node.Location);
                return base.Visit(node, context);
            }
        }
    }

    
}
