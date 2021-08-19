using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Visitors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Visitors
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
            patientNode.RemoveEmptyNodes();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            Assert.Null(patientAddress);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenACryptoHashRule_WhenProcess_NodeShouldBeHashed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.address", "address", "Patient", "cryptoHash", AnonymizerRuleType.FhirPathRule, "Patient.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            var patientAddress = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            Assert.Equal("c4321653de997f3029d2efa38dd4baa6c9c2f6bd67b8a52be789f157f8b286ce", patientAddress.Value.ToString());

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.CRYTOHASH.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAnEncryptRule_WhenProcess_NodeShouldBeEncrypted()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.address", "address", "Patient", "encrypt", AnonymizerRuleType.FhirPathRule, "Patient.address"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            var patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            var key = Encoding.UTF8.GetBytes("1234567890123456");
            var plainValue = EncryptUtility.DecryptTextFromBase64WithAes(patientCity.Value.ToString(), key);
            Assert.Equal("patienttestcity1", plainValue);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.ENCRYPT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAPrimitiveSubstituteRule_WhenProcess_NodeShouldBeSubstituted()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.address.city", "address.city", "Patient", "substitute", AnonymizerRuleType.FhirPathRule, "Patient.address.city",
                    new Dictionary<string, object> { {"replaceWith", "ExampleCity2020" } })           
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            var patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            Assert.Equal("patienttestcity1", patientCity.Value.ToString());
            var patientCountry = patientNode.Select("Patient.address[0].country").FirstOrDefault();
            Assert.Equal("patienttestcountry1", patientCountry.Value.ToString());

            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            Assert.Equal("ExampleCity2020", patientCity.Value.ToString());
            patientCountry = patientNode.Select("Patient.address[0].country").FirstOrDefault();
            Assert.Equal("patienttestcountry1", patientCountry.Value.ToString());
            var patientCity2 = patientNode.Select("Patient.contact[0].address[0].city").FirstOrDefault();
            Assert.Equal("patienttestcity2", patientCity2.Value.ToString());

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.SUBSTITUTED.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAComplexSubstituteRule_WhenProcess_NodeShouldBeSubstituted()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Patient.address", "address", "Patient", "substitute", AnonymizerRuleType.FhirPathRule, "Patient.address",
                    new Dictionary<string, object> { {"replaceWith", "{ \"city\": \"ExampleCity2020\" }" } } ),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            var patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            Assert.Equal("patienttestcity1", patientCity.Value.ToString());
            var patientCountry = patientNode.Select("Patient.address[0].country").FirstOrDefault();
            Assert.Equal("patienttestcountry1", patientCountry.Value.ToString());

            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            Assert.Equal("ExampleCity2020", patientCity.Value.ToString());
            patientCountry = patientNode.Select("Patient.address[0].country").FirstOrDefault();
            Assert.Null(patientCountry);
            var patientCity2 = patientNode.Select("Patient.contact[0].address[0].city").FirstOrDefault();
            Assert.Equal("patienttestcity2", patientCity2.Value.ToString());

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.SUBSTITUTED.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAPerturbRule_WhenProcess_NodeShouldBePerturbed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Observation.referenceRange.low", "referenceRange.low", "Observation", "perturb", AnonymizerRuleType.FhirPathRule, "Observation.referenceRange.low",
                    new Dictionary<string, object> { { "span", "4" } }),
                new AnonymizationFhirPathRule("Observation.referenceRange.high.value", "referenceRange.high.value", "Observation", "perturb", AnonymizerRuleType.FhirPathRule, "Observation.referenceRange.high.value",
                    new Dictionary<string, object> { { "span", "0.2" }, { "rangeType", "proportional" } })
            };
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var observation = CreateTestObservation();
            var observationNode = ElementNode.FromElement(observation.ToTypedElement());
            observationNode.Accept(visitor);
            observationNode.RemoveEmptyNodes();

            var lowNode = observationNode.Select("Observation.referenceRange.low");
            var perturbedValue = decimal.Parse(lowNode.Children("value").First().Value.ToString());
            Assert.InRange(perturbedValue, 8, 12);

            var highNode = observationNode.Select("Observation.referenceRange.high");
            perturbedValue = decimal.Parse(highNode.Children("value").First().Value.ToString());
            Assert.InRange(perturbedValue, 90, 110);

            observation = observationNode.ToPoco<Observation>();
            Assert.Contains(SecurityLabels.PERTURBED.Code, observation.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAPerturbRuleAndThenARedactRule_WhenProcess_NodeShouldBePerturbed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Observation.referenceRange.low", "referenceRange.low", "Observation", "perturb", AnonymizerRuleType.FhirPathRule, "Observation.referenceRange.low",
                    new Dictionary<string, object> { { "span", "2" } }),
                new AnonymizationFhirPathRule("Observation.referenceRange.low", "referenceRange.low", "Observation", "redact", AnonymizerRuleType.FhirPathRule, "Observation.referenceRange.low")
            };
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var observation = CreateTestObservation();
            var observationNode = ElementNode.FromElement(observation.ToTypedElement());
            observationNode.Accept(visitor);

            var lowNode = observationNode.Select("Observation.referenceRange.low");
            var perturbedValue = decimal.Parse(lowNode.Children("value").First().Value.ToString());
            Assert.InRange(perturbedValue, 9, 11);

            var unitNode = observationNode.Select("Observation.referenceRange.low.unit").First();
            Assert.Equal("TestUnit", unitNode.Value.ToString());

            observation = observationNode.ToPoco<Observation>();
            Assert.Contains(SecurityLabels.PERTURBED.Code, observation.Meta.Security.Select(s => s.Code));
            Assert.DoesNotContain(SecurityLabels.REDACT.Code, observation.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenARedactRuleAndThenAPerturbRule_WhenProcess_NodeShouldBeRedacted()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Observation.referenceRange.low.value", "referenceRange.low.value", "Observation", "redact", AnonymizerRuleType.FhirPathRule, "Observation.referenceRange.low.value"),
                new AnonymizationFhirPathRule("Observation.referenceRange.low", "referenceRange.low", "Observation", "perturb", AnonymizerRuleType.FhirPathRule, "Observation.referenceRange.low",
                    new Dictionary<string, object> { { "span", "2" } }),
            };
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var observation = CreateTestObservation();
            var observationNode = ElementNode.FromElement(observation.ToTypedElement());
            observationNode.Accept(visitor);

            var lowNode = observationNode.Select("Observation.referenceRange.low");
            var perturbedValue = lowNode.Children("value").First().Value;
            Assert.Null(perturbedValue);

            observation = observationNode.ToPoco<Observation>();
            Assert.Contains(SecurityLabels.REDACT.Code, observation.Meta.Security.Select(s => s.Code));
            Assert.DoesNotContain(SecurityLabels.PERTURBED.Code, observation.Meta.Security.Select(s => s.Code));
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
            patientNode.RemoveEmptyNodes();

            var patientId = patientNode.Select("Patient.id").FirstOrDefault();
            Assert.Null(patientId);

            patient = patientNode.ToPoco<Patient>();
            Assert.Single(patient.Meta.Security);
            Assert.Contains(SecurityLabels.REDACT.Code, patient.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void Given2ConflictRules_WhenProcess_SecondRuleShouldBeIgnored()
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
            patientNode.RemoveEmptyNodes();
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
            bundleNode.RemoveEmptyNodes();
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
            bundleNode.RemoveEmptyNodes();
            person = bundleNode.Select("Bundle.entry[0].resource[0]").FirstOrDefault().ToPoco<Person>();

            Assert.NotNull(person);
            Assert.NotNull(person.Meta);
            Assert.Null(person.Active);
        }

        [Fact]
        public void GivenANodesByTypeRule_WhenProcess_AllNodesShouldBeProcessed()
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
            patientNode.RemoveEmptyNodes();

            var personAddress = patientNode.Select("Patient.contained[0].contained[0].address[0]").FirstOrDefault();
            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();

            Assert.Null(personAddress);
            Assert.Null(patientAddress);
        }

        [Fact]
        public void GivenANodesByTypeRuleWithResourceType_WhenProcess_OnlyNodesInSpecificResourceTypeShouldBeProcessed()
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
            patientNode.RemoveEmptyNodes();

            var personAddress = patientNode.Select("Patient.contained[0].contained[0].address[0]").FirstOrDefault();
            string patientCity = patientNode.Select("Patient.address[0].city").First().Value.ToString();
            string patientCountry = patientNode.Select("Patient.address[0].country").First().Value.ToString();

            Assert.Equal("patienttestcity1", patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);
            Assert.Null(personAddress);
        }

        [Fact]
        public void GivenANodesByTypeRuleWithExpression_WhenProcess_OnlyNodesMatchSpecificExpressionShouldBeProcessed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("nodesByType('Address').city", "nodesByType('Address').city", "", "redact", AnonymizerRuleType.FhirPathRule, "nodesByType('Address').city"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            var patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            var patientCountry = patientNode.Select("Patient.address[0].country").First().Value.ToString();

            Assert.NotNull(patientAddress);
            Assert.Null(patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);
        }

        [Fact]
        public void GivenANodesByTypeRuleWithExpressionFunction_WhenProcess_OnlyNodesMatchSpecificExpressionFunctionShouldBeProcessed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("nodesByType('Address').where(city='patienttestcity1').city", "nodesByType('Address').where(city='patienttestcity1').city", "", "redact", AnonymizerRuleType.FhirPathRule, "nodesByType('Address').where(city='patienttestcity1').city"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            var patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            var patientCountry = patientNode.Select("Patient.address[0].country").First().Value.ToString();

            Assert.NotNull(patientAddress);
            Assert.Null(patientCity);
            Assert.Equal("patienttestcountry1", patientCountry);
        }

        [Fact]
        public void GivenAKeepRuleAndThenANodesByTypeRule_WhenProcess_NodesShouldBeProcessedCorrectly()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("Person.address.country", "Person.address.country", "Person", "keep", AnonymizerRuleType.FhirPathRule, "Person.address.country"),
                new AnonymizationFhirPathRule("nodesByType('Address')", "nodesByType('Address')", "", "redact", AnonymizerRuleType.FhirPathRule, "nodesByType('Address')"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var person = CreateTestPerson();
            patient.Contained.Add(person);

            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            var patientAddress = patientNode.Select("Patient.address[0]").FirstOrDefault();
            var personAddress = patientNode.Select("Patient.contained[0].address[0]").FirstOrDefault();
            var personCity = patientNode.Select("Patient.contained[0].address[0].city").FirstOrDefault();
            var personCountry = patientNode.Select("Patient.contained[0].address[0].country").First().Value.ToString();

            Assert.Null(patientAddress);
            Assert.NotNull(personAddress);
            Assert.Null(personCity);
            Assert.Equal("persontestcountry", personCountry);
        }

        [Fact]
        public void GivenANodesByNameRule_WhenProcess_AllNodesShouldBeProcessed()
        {
            AnonymizationFhirPathRule[] rules = new AnonymizationFhirPathRule[]
            {
                new AnonymizationFhirPathRule("nodesByName('city')", "nodesByName('city')", "", "redact", AnonymizerRuleType.FhirPathRule, "nodesByName('city')"),
            };

            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, CreateTestProcessors());

            var patient = CreateTestPatient();
            var organization = CreateTestOrganization();
            var person = CreateTestPerson();

            patient.Contained.Add(organization);
            organization.Contained.Add(person);

            var patientNode = ElementNode.FromElement(patient.ToTypedElement());
            patientNode.Accept(visitor);
            patientNode.RemoveEmptyNodes();

            var personCity = patientNode.Select("Patient.contained[0].contained[0].address[0].city").FirstOrDefault();
            var personCountry = patientNode.Select("Patient.contained[0].contained[0].address[0].country").First().Value.ToString();
            var organizationCity = patientNode.Select("Patient.contained[0].address[0].city").FirstOrDefault();
            var organizationCountry = patientNode.Select("Patient.contained[0].address[0].country").First().Value.ToString();
            var patientCity = patientNode.Select("Patient.address[0].city").FirstOrDefault();
            var patientCountry = patientNode.Select("Patient.address[0].country").First().Value.ToString();

            Assert.Null(personCity);
            Assert.Null(organizationCity);
            Assert.Null(patientCity);
            Assert.Equal("persontestcountry", personCountry);
            Assert.Equal("OrgTestCountry", organizationCountry);
            Assert.Equal("patienttestcountry1", patientCountry);
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
            bundleNode.RemoveEmptyNodes();

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
            patientNode.RemoveEmptyNodes();

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
            patientNode.RemoveEmptyNodes();

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
            CryptoHashProcessor cryptoHashProcessor = new CryptoHashProcessor("123");
            EncryptProcessor encryptProcessor = new EncryptProcessor("1234567890123456");
            SubstituteProcessor substituteProcessor = new SubstituteProcessor();
            PerturbProcessor perturbProcessor = new PerturbProcessor();
            Dictionary<string, IAnonymizerProcessor> processors = new Dictionary<string, IAnonymizerProcessor>()
            {
                { "KEEP", keepProcessor},
                { "REDACT", redactProcessor},
                { "DATESHIFT", dateShiftProcessor},
                { "CRYPTOHASH", cryptoHashProcessor},
                { "ENCRYPT", encryptProcessor },
                { "SUBSTITUTE", substituteProcessor },
                { "PERTURB", perturbProcessor }
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

        private Observation CreateTestObservation()
        {
            Observation observation = new Observation();
            observation.ReferenceRange.Add(
                new Observation.ReferenceRangeComponent
                {
                    Low = new Quantity { Value = 10, Unit = "TestUnit" },
                    High = new Quantity { Value = 100},
                });
            return observation;
        }
    }
}
