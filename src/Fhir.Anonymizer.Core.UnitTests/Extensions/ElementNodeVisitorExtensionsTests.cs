using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Extensions
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
            var result = new HashSet<string>();
            node.Accept(new TestVisitor(result));

            Assert.Equal(5, result.Count);
            Assert.Contains("Patient", result);
            Assert.Contains("Patient.active[0]", result);
            Assert.Contains("Patient.address[0]", result);
            Assert.Contains("Patient.address[0].city[0]", result);
            Assert.Contains("Patient.address[1]", result);
        }

        [Fact]
        public void GivenAPatientNodeWithContained_WhenVisit_AllNodesShouldBeVisited()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test" });
            patient.Address.Add(new Address());
            patient.Contained.Add(new Observation() {  Status = ObservationStatus.Unknown });

            var node = ElementNode.FromElement(patient.ToTypedElement());
            var result = new HashSet<string>();
            node.Accept(new TestVisitor(result));

            Assert.Equal(7, result.Count);
            Assert.Contains("Patient", result);
            Assert.Contains("Patient.active[0]", result);
            Assert.Contains("Patient.address[0]", result);
            Assert.Contains("Patient.address[0].city[0]", result);
            Assert.Contains("Patient.address[1]", result);
            Assert.Contains("Patient.contained[0]", result);
            Assert.Contains("Patient.contained[0].status[0]", result);
        }

        [Fact]
        public void GivenABundleNode_WhenVisit_AllNodesShouldBeVisited()
        {
            Bundle bundle = new Bundle();
            bundle.Timestamp = new DateTimeOffset();
            
            Patient patient = new Patient();
            patient.Active = true;

            bundle.AddResourceEntry(patient, "http://example.org/fhir/Patient/1");

            var node = ElementNode.FromElement(bundle.ToTypedElement());
            var result = new HashSet<string>();
            node.Accept(new TestVisitor(result));

            Assert.Equal(6, result.Count);
            Assert.Contains("Bundle", result);
            Assert.Contains("Bundle.timestamp[0]", result);
            Assert.Contains("Bundle.entry[0].fullUrl[0]", result);
            Assert.Contains("Bundle.entry[0]", result);
            Assert.Contains("Bundle.entry[0].resource[0]", result);
            Assert.Contains("Bundle.entry[0].resource[0].active[0]", result);
        }

        private class TestVisitor : AbstractElementNodeVisitor
        {
            private HashSet<string> _result;
            public TestVisitor(HashSet<string> result)
            {
                _result = result;
            }

            public override async Task<bool> Visit(ElementNode node)
            {
                _result.Add(node.Location);
                return true;
            }
        }
    }

    
}
