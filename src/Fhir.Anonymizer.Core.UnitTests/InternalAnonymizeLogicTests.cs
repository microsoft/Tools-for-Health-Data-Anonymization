using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class InternalAnonymizeLogicTests
    {
        public InternalAnonymizeLogicTests()
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

            InternalAnonymizeLogic logic = new InternalAnonymizeLogic(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var result = logic.Anonymize(ElementNode.FromElement(patient.ToTypedElement()));
            var patientCity = result.Select("Patient.address[0].city").First().Value;
            var patientCountry = result.Select("Patient.address[0].country").First().Value;

            Assert.Null(patientCity);
            Assert.Null(patientCountry);
        }

        [Fact]
        public void Given2ConflictRules_WhenProcess_SecondRuleShouldBeOverride()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient", "Patient", "Patient", "keep", AnonymizerRuleType.FhirPathRule, "Patient"),
                new AnonymizationFhirPathRule("Patient.address", "address", "Patient", "redact", AnonymizerRuleType.FhirPathRule, "Patient.address"),
            };

            InternalAnonymizeLogic logic = new InternalAnonymizeLogic(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var result = logic.Anonymize(ElementNode.FromElement(patient.ToTypedElement()));
            string patientCity = result.Select("Patient.address[0].city").First().Value.ToString();
            string patientCountry = result.Select("Patient.address[0].country").First().Value.ToString();

            Assert.Equal("patienttestcity1", patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);
        }

        [Fact]
        public void GivenAFhirPathMatchContianedNode_WhenProcess_NodesInContainedShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.address", "address", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.address"),
            };

            InternalAnonymizeLogic logic = new InternalAnonymizeLogic(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var result = logic.Anonymize(ElementNode.FromElement(patient.ToTypedElement()));
            var personCity = result.Select("Patient.contained[0].contained[0].address[0].city[0]").First().Value;
            var personCountry = result.Select("Patient.contained[0].contained[0].address[0].country[0]").First().Value;

            Assert.Null(personCity);
            Assert.Null(personCountry);
        }

        [Fact]
        public void GivenAFhirPathMatchBundleEntryNode_WhenProcess_NodesInBundleShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.address", "address", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.address"),
            };

            InternalAnonymizeLogic logic = new InternalAnonymizeLogic(rules, CreateTestProcessors());

            var person = CreateTestPerson();
            var bundle = new Bundle();
            bundle.AddResourceEntry(person, "http://example.org/fhir/Person/1");
            var result = logic.Anonymize(ElementNode.FromElement(bundle.ToTypedElement()));
            var personCity = result.Select("Bundle.entry[0].resource[0].address[0].city[0]").First().Value;
            var personCountry = result.Select("Bundle.entry[0].resource[0].address[0].country[0]").First().Value;

            Assert.Null(personCity);
            Assert.Null(personCountry);
        }

        [Fact]
        public void GivenARuleWithNodeByTypeAndResourceType_WhenProcess_OnlyNodeInSpecificResourceTypeShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.nodesByType('Address')", "nodesByType('Address')", "Person", "redact", AnonymizerRuleType.FhirPathRule, "Person.nodesByType('Address')"),
            };

            InternalAnonymizeLogic logic = new InternalAnonymizeLogic(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var result = logic.Anonymize(ElementNode.FromElement(patient.ToTypedElement()));
            var personCity = result.Select("Patient.contained[0].contained[0].address[0].city[0]").First().Value;
            var personCountry = result.Select("Patient.contained[0].contained[0].address[0].country[0]").First().Value;
            string patientCity = result.Select("Patient.address[0].city").First().Value.ToString();
            string patientCountry = result.Select("Patient.address[0].country").First().Value.ToString();

            Assert.Equal("patienttestcity1", patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);
            Assert.Null(personCity);
            Assert.Null(personCountry);
        }

        [Fact]
        public void GivenARuleWitoutResourceType_WhenProcess_AllNodesShouldBeRedact()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("nodesByType('Address')", "nodesByType('Address')", "", "redact", AnonymizerRuleType.FhirPathRule, "nodesByType('Address')"),
            };

            InternalAnonymizeLogic logic = new InternalAnonymizeLogic(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var result = logic.Anonymize(ElementNode.FromElement(patient.ToTypedElement()));
            var personCity = result.Select("Patient.contained[0].contained[0].address[0].city[0]").First().Value;
            var personCountry = result.Select("Patient.contained[0].contained[0].address[0].country[0]").First().Value;
            var patientCity = result.Select("Patient.address[0].city").First().Value;
            var patientCountry = result.Select("Patient.address[0].country").First().Value;

            Assert.Null(patientCity);
            Assert.Null(patientCountry);
            Assert.Null(personCity);
            Assert.Null(personCountry);
        }

        private Dictionary<string, IAnonymizerProcessor> CreateTestProcessors()
        {
            KeepProcessor keepProcessor = new KeepProcessor();
            RedactProcessor redactProcessor = new RedactProcessor(false, false, false, null);
            Dictionary<string, IAnonymizerProcessor> processors = new Dictionary<string, IAnonymizerProcessor>()
            {
                { "KEEP", keepProcessor},
                { "REDACT", redactProcessor}
            };

            return processors;
        }

        private Patient CreateTestPatient()
        {
            Patient patient = new Patient();

            patient.Address.Add(new Address() { City = "patienttestcity1", Country = "patienttestcountry1" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "patienttestcity2", Country = "patienttestcountry2", PostalCode = "12345" } });
            patient.Contained.Add(CreateTestOrganization());

            return patient;
        }

        private Organization CreateTestOrganization()
        {
            Organization organization = new Organization();

            organization.Name = "TestOrganization";
            organization.Address.Add(new Address() { City = "OrgTestCity", Country = "OrgTestCountry" });
            organization.Contained.Add(CreateTestPerson());

            return organization;
        }

        private Person CreateTestPerson()
        {
            Person person = new Person();

            person.Address.Add(new Address() { City = "persontestcity", Country = "persontestcountry"});

            return person;
        }
    }
}
