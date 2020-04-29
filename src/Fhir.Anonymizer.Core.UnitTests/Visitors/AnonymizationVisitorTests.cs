using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Visitors
{
    public class AnonymizationVisitorTests
    {
        public AnonymizationVisitorTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        [Fact]
        public void GivenARedactRule_WhenProcess_NodeShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.address", "address", "Patient", "redact", AnonymizerRuleType.FhirPathRule, "Patient.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            Assert.Null(patientAddress);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAPatientWithOnlyId_WhenProcess_NodeShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.id", "id", "Patient", "redact", AnonymizerRuleType.FhirPathRule, "Patient.id"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = new Patient();
            patient.Id = "Test";
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();

            var patientId = patientNode.Select("Patient.id").FirstOrDefault();
            Assert.Null(patientId);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void Given2ConflictRules_WhenProcess_SecondRuleShouldBeOverridden()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient", "Patient", "Patient", "keep", AnonymizerRuleType.FhirPathRule, "Patient"),
                new AnonymizationFhirPathRule("Patient.address", "address", "Patient", "redact", AnonymizerRuleType.FhirPathRule, "Patient.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor); 
            string patientCity = patientNode.Select("Patient.address[0].city").First().Value.ToString();
            string patientCountry = patientNode.Select("Patient.address[0].country").First().Value.ToString();

            Assert.Equal("patienttestcity1", patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);

            patient = patientNode.ToPoco<Patient>();
            Assert.Null(patient.Meta);
        }

        [Fact]
        public void GivenAFhirPathMatchContainedNode_WhenProcess_NodesInContainedShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.address.city", "address.city", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var org = CreateTestOrganization();
            var person = CreateTestPerson();

            patient.Contained.Add(org);
            org.Contained.Add(person);

            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();
            var personCity = patientNode.Select("Patient.contained[0].contained[0].address[0].city[0]").FirstOrDefault();

            Assert.Null(personCity);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));

            Assert.Null(patient.Contained[0].Meta);
        }

        [Fact]
        public void GivenAFhirPathMatchBundleEntryNode_WhenProcess_NodesInBundleShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.address.city", "address.city", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var person = CreateTestPerson();
            var bundle = new Bundle();
            bundle.AddResourceEntry(person, "http://example.org/fhir/Person/1");

            var bundleNode = ElementNode.FromElement(bundle.ToTypedElement());
            bundleNode.Accept(visitor);
            bundleNode.RemoveNullChildren();
            var personCity = bundleNode.Select("Bundle.entry[0].resource[0].address[0].city[0]").FirstOrDefault();

            Assert.Null(personCity);
        }

        [Fact]
        public void GivenAResourceTypeRuleMatchBundleEntryNode_WhenProcess_NodesInBundleShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person", "Person", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var person = CreateTestPerson();
            var bundle = new Bundle();
            bundle.AddResourceEntry(person, "http://example.org/fhir/Person/1");

            var bundleNode = ElementNode.FromElement(bundle.ToTypedElement());
            bundleNode.Accept(visitor);
            bundleNode.RemoveNullChildren();
            person = bundleNode.Select("Bundle.entry[0].resource[0]").FirstOrDefault().ToPoco<Person>();

            Assert.NotNull(person);
            Assert.NotNull(person.Meta);
            Assert.Null(person.Active);
        }

        [Fact]
        public void GivenARuleWithNodeByTypeAndResourceType_WhenProcess_OnlyNodeInSpecificResourceTypeShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.nodesByType('Address')", "nodesByType('Address')", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.nodesByType('Address')"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var org = CreateTestOrganization();
            var person = CreateTestPerson();

            patient.Contained.Add(org);
            org.Contained.Add(person);

            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();

            var personAddress = patientNode.Select("Patient.contained[0].contained[0].address[0]").FirstOrDefault();
            string patientCity = patientNode.Select("Patient.address[0].city").First().Value.ToString();
            string patientCountry = patientNode.Select("Patient.address[0].country").First().Value.ToString();

            Assert.Equal("patienttestcity1", patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);
            Assert.Null(personAddress);
        }

        [Fact]
        public void GivenARuleWithoutResourceType_WhenProcess_AllNodesShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("nodesByType('Address')", "nodesByType('Address')", "", "redact", AnonymizerRuleType.FhirPathRule, "nodesByType('Address')"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var org = CreateTestOrganization();
            var person = CreateTestPerson();

            patient.Contained.Add(org);
            org.Contained.Add(person);

            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();
            var personAddress = patientNode.Select("Patient.contained[0].contained[0].address[0]").FirstOrDefault();
            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();

            Assert.Null(personAddress);
            Assert.Null(patientAddress);
        }

        [Fact]
        public void GivenABundleWith2RuleOfDifferentMethods_WhenProcess_SecurityTagShouldBeAddedCorrectly()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.nodesByType('date')", "nodesByType('date')", "", "dateshift", AnonymizerRuleType.FhirPathRule, "Patient.nodesByType('date')"),
                new AnonymizationFhirPathRule("Person.nodesByType('Address')", "nodesByType('Address')", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.nodesByType('Address')"),
            };

            var person = CreateTestPerson();
            var patient = CreateTestPatient();
            var bundle = new Bundle();
            bundle.AddResourceEntry(person, "http://example.org/fhir/Person/1");
            bundle.AddResourceEntry(patient, "http://example.org/fhir/Patient/1");

            var bundleNode = ElementNode.FromElement(bundle.ToTypedElement());
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            bundleNode.Accept(visitor);
            bundleNode.RemoveNullChildren();

            bundle = bundleNode.ToPoco<Bundle>();
            Assert.Equal(2, bundle.Meta.Security.Count);
            Assert.Contains(SecurityLabels.REDACT.Code, bundle.Meta.Security.Select(s => s.Code));
            Assert.Contains(SecurityLabels.PERTURBED.Code, bundle.Meta.Security.Select(s => s.Code));

            Resource resource = bundle.Entry[0].Resource;
            Assert.Single(resource.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, resource.Meta.Security.Select(s => s.Code));

            resource = bundle.Entry[1].Resource;
            Assert.Single(resource.Meta.Security);
            Assert.Contains(SecurityLabels.PERTURBED.Code, resource.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenARuleWithGeneralType_WhenProcess_AllTypeNodesShouldBeProcessed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Resource.address", "address", "Patient", "redact", AnonymizerRuleType.FhirPathRule, "Patient.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            Assert.Null(patientAddress);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenARuleForAll_WhenProcess_AllTypeNodesShouldBeProcessed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Resource", "Resource", "Resource", "redact", AnonymizerRuleType.FhirPathRule, "Resource"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveNullChildren();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            Assert.Null(patientAddress);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        private Dictionary<string, IAnonymizerProcessor> CreateTestProcessors()
        {
            KeepProcessor keepProcessor = new KeepProcessor();
            RedactProcessor redactProcessor = new RedactProcessor(false, false, false, null);
            DateShiftProcessor dateShiftProcessor = new DateShiftProcessor("123", "123", false);
            Dictionary<string, IAnonymizerProcessor> processors = new Dictionary<string, IAnonymizerProcessor>()
            {
                { "KEEP", keepProcessor},
                { "REDACT", redactProcessor},
                { "DATESHIFT", dateShiftProcessor}
            };

            return processors;
        }

        private Patient CreateTestPatient()
        {
            Patient patient = new Patient();

            patient.Address.Add(new Address() { City = "patienttestcity1", Country = "patienttestcountry1", District = "TestDistrict" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "patienttestcity2", Country = "patienttestcountry2", PostalCode = "12345" } });
            patient.Active = true;
            patient.BirthDateElement = new Date(2000, 1, 1);

            return patient;
        }

        private Organization CreateTestOrganization()
        {
            Organization organization = new Organization();

            organization.Name = "TestOrganization";
            organization.Address.Add(new Address() { City = "OrgTestCity", Country = "OrgTestCountry", District = "TestDistrict" });
            organization.Active = true;

            return organization;
        }

        private Person CreateTestPerson()
        {
            Person person = new Person();

            person.Address.Add(new Address() { City = "persontestcity", Country = "persontestcountry", District = "TestDistrict" });
            person.Active = true;

            return person;
        }
    }
}
