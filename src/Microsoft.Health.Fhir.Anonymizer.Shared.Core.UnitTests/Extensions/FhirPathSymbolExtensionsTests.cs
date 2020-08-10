using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Extensions
{
    public class FhirPathSymbolExtensionsTests
    {
        public FhirPathSymbolExtensionsTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        [Fact]
        public void GivenListOfElementNodes_WhenGetDecendantsByType_AllNodesShouldBeReturned()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test0" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "Test1" } });
            Address address = new Address() { City = "Test2" };

            Organization organizaton = new Organization();
            organizaton.Address.Add(new Address() { City = "Test3"} );

            // contained resource should not be returned.
            Organization organizatonInContained = new Organization();
            organizatonInContained.Address.Add(new Address() { City = "Test3" });
            patient.Contained.Add(organizatonInContained);

            // Verify primitive object
            Date date = new Date();

            var nodes = new ITypedElement[] { patient .ToTypedElement(), address.ToTypedElement(), organizaton.ToTypedElement(), date.ToTypedElement()}.Select(n => ElementNode.FromElement(n));
            var results = FhirPathSymbolExtensions.NodesByType(nodes, "Address").Select(n => n.Location);

            Assert.Equal(4, results.Count());
            Assert.Contains("Patient.address[0]", results);
            Assert.Contains("Address", results);
            Assert.Contains("Organization.address[0]", results);
            Assert.Contains("Patient.contact[0].address[0]", results);
        }

        [Fact]
        public void GivenListOfPrimitiveElementNodes_WhenGetDecendantsByType_AllNodesShouldBeReturned()
        {
            Date date = new Date();
            Instant instant = new Instant();
            FhirBoolean boolean = new FhirBoolean();
            FhirString fhirString = new FhirString();

            var nodes = new Primitive[] { date, instant, boolean, fhirString }.Select(n => ElementNode.FromElement(n.ToTypedElement()));
            
            var results = FhirPathSymbolExtensions.NodesByType(nodes, "string").Select(n => n.Location);
            Assert.Single(results);
            Assert.Contains("string", results);

            results = FhirPathSymbolExtensions.NodesByType(nodes, "date").Select(n => n.Location);
            Assert.Single(results);
            Assert.Contains("date", results);

            results = FhirPathSymbolExtensions.NodesByType(nodes, "boolean").Select(n => n.Location);
            Assert.Single(results);
            Assert.Contains("boolean", results);

            results = FhirPathSymbolExtensions.NodesByType(nodes, "instant").Select(n => n.Location);
            Assert.Single(results);
            Assert.Contains("instant", results);
        }

        [Fact]
        public void GivenListOfElementNodes_WhenGetDecendantsByName_AllNodesShouldBeReturned()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test0" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "Test1" } });
            Address address = new Address() { City = "Test2" };

            Organization organizaton = new Organization();
            organizaton.Address.Add(new Address() { City = "Test3" });

            // contained resource should not be returned.
            Organization organizatonInContained = new Organization();
            organizatonInContained.Address.Add(new Address() { City = "Test3" });
            patient.Contained.Add(organizatonInContained);

            // Verify primitive object
            Date date = new Date();

            var nodes = new ITypedElement[] { patient.ToTypedElement(), address.ToTypedElement(), organizaton.ToTypedElement(), date.ToTypedElement() }.Select(n => ElementNode.FromElement(n));
            var results = FhirPathSymbolExtensions.NodesByName(nodes, "address").Select(n => n.Location);

            Assert.Equal(3, results.Count());
            Assert.Contains("Patient.address[0]", results);
            Assert.Contains("Organization.address[0]", results);
            Assert.Contains("Patient.contact[0].address[0]", results);
        }

        [Fact]
        public void GivenAPatient_WhenNavigateWithExtendedFunction_MatchNodeShouldBeReturned()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test0" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "Test1" } });

            int resultCount = ElementNode.FromElement(patient.ToTypedElement()).Select("Patient.nodesByName('address')").Count();
            Assert.Equal(2, resultCount);

            resultCount = ElementNode.FromElement(patient.ToTypedElement()).Select("Patient.nodesByType('Address')").Count();
            Assert.Equal(2, resultCount);
        }
    }
}
