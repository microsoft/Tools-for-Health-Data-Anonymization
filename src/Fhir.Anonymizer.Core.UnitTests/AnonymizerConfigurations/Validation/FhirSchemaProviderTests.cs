using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations.Validation
{
    public class FhirSchemaProviderTests
    {
        private readonly FhirSchemaProvider _fhirSchemaProvider = new FhirSchemaProvider();
        private readonly Dictionary<string, HashSet<string>> _anonymizationMethodTargetTypes;

        public FhirSchemaProviderTests()
        {
            _anonymizationMethodTargetTypes = new Dictionary<string, HashSet<string>>();

            var anonymizationMethodNames = Enum.GetNames(typeof(AnonymizerMethod)).Select(name => name.ToLower());
            foreach (string methodName in anonymizationMethodNames)
            {
                if (string.Equals(methodName, "dateshift", StringComparison.InvariantCultureIgnoreCase))
                {
                    _anonymizationMethodTargetTypes.Add(methodName,
                        new HashSet<string> { "date", "dateTime", "instant" });
                }
                else
                {
                    _anonymizationMethodTargetTypes.Add(methodName, _fhirSchemaProvider.GetFhirAllTypes());
                }
            }
        }

        public static IEnumerable<object[]> GetValidPathRules()
        {
            yield return new object[] { "Resource.id", "redact", "id" };
            yield return new object[] { "Patient.contained", "keep", "Resource" };
            yield return new object[] { "Patient.name.family", "redact", "string" };
            yield return new object[] { "Patient.address.country", "keep", "string" };
            yield return new object[] { "Patient.birthDate", "dateshift", "date" };
            yield return new object[] { "Observation.issued", "dateshift", "instant" };
            yield return new object[] { "Condition.onset.start", "redact", "dateTime" };
            yield return new object[] { "MedicationRequest.reported.display", "redact", "string" };
        }

        public static IEnumerable<object[]> GetInvalidPathRules()
        {
            yield return new object[] { "....", "redact", ".... is invalid." };
            yield return new object[] { ".", "redact", ". is invalid." };
            yield return new object[] { "HumanName.family", "redact", "HumanName is an invalid resource type." };
            yield return new object[] { "name.family", "redact", "name is an invalid resource type." };
            yield return new object[] { "Patient.name.families", "redact", "families is an invalid field in Patient.name." };
            yield return new object[] { "Patient.mood", "redact", "mood is an invalid field in Patient." };
            yield return new object[] { "Organization.name", "delete", "Anonymization method delete is currently not supported." };
            yield return new object[] { "Bundle.entry.resource", "redact", "Path of Bundle/contained resources is not supported." };
            yield return new object[] { "Patient.contained.id", "redact", "Path of Bundle/contained resources is not supported." };
        }

        public static IEnumerable<object[]> GetValidTypeRules()
        {
            yield return new object[] { "HumanName.family", "redact", "string" };
            yield return new object[] { "HumanName.use", "keep", "code" };
            yield return new object[] { "date", "dateshift", "date" };
            yield return new object[] { "dateTime", "dateshift", "dateTime" };
            yield return new object[] { "instant", "dateshift", "instant" };
            yield return new object[] { "CodeableConcept.text", "redact", "string" };
            yield return new object[] { "Reference.display", "redact", "string" };
            //return first data type for choice element
            yield return new object[] { "QuestionnaireResponse*item.answer.value", "redact", "boolean" }; 
        }

        public static IEnumerable<object[]> GetInvalidTypeRules()
        {
            yield return new object[] { "....", "redact", ".... is invalid." };
            yield return new object[] { ".", "redact", ". is invalid." };
            yield return new object[] { "Name.families", "redact", "Name is an invalid data type." };
            yield return new object[] { "Resource.text", "redact", "Resource is an invalid data type." };
            yield return new object[] { "HumanName.families", "redact", "families is an invalid field in HumanName." };
            yield return new object[] { "HumanName.use", "dateshift", "Anonymization method dateshift cannot be applied to HumanName.use." };
            yield return new object[] { "BackboneElement.answer.value", "redact", "BackboneElement is a valid but not supported data type." };
            yield return new object[] { "Address.state", "delete", "Anonymization method delete is currently not supported." };
        }

        [Fact]
        public void GivenSchema_WhenInitialized_ThenCorrectSchemaShouldBeSet()
        {
            var resources = _fhirSchemaProvider.GetFhirResourceTypes();
            Assert.Equal(148, resources.Count);
            Assert.Contains("Patient", resources);
            Assert.Contains("Observation", resources);
            Assert.Contains("Bundle", resources);

            var types = _fhirSchemaProvider.GetFhirDataTypes();
            Assert.Equal(535, types.Count);
            Assert.Contains("HumanName", types); 
            Assert.Contains("Address", types);
            Assert.Contains("date", types);
            Assert.Contains("CodeableConcept", types);
            Assert.Contains("Patient*contact", types);
            Assert.Contains("QuestionnaireResponse*item", types);
            Assert.Contains("BackboneElement", types);
            Assert.DoesNotContain("Resource", types);

            var schema = _fhirSchemaProvider.GetFhirSchema();
            Assert.Equal(683, schema.Count);
        }

        [Theory]
        [MemberData(nameof(GetValidTypeRules))]
        public void GivenAValidTypeRule_WhenValidate_ResultShouldBeSuccess(string path, string method, string expectedTargetDataType)
        {
            var result = _fhirSchemaProvider.ValidateRule(path, method, AnonymizerRuleType.TypeRule, _anonymizationMethodTargetTypes.GetValueOrDefault(method));
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(expectedTargetDataType, result.TargetDataType);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTypeRules))]
        public void GivenAnInValidTypeRule_WhenValidate_ResultShouldNotBeSuccess_AndErrorMessageShouldBeCorrect(string path, string method, string expectedErrorMessage)
        {
            var result = _fhirSchemaProvider.ValidateRule(path, method, AnonymizerRuleType.TypeRule, _anonymizationMethodTargetTypes.GetValueOrDefault(method));
            Assert.False(result.Success);
            Assert.Equal(expectedErrorMessage, result.ErrorMessage);
        }

        [Theory]
        [MemberData(nameof(GetValidPathRules))]
        public void GivenAValidPathRule_WhenValidate_ResultShouldBeSuccess(string path, string method, string expectedTargetDataType)
        {
            var result = _fhirSchemaProvider.ValidateRule(path, method, AnonymizerRuleType.PathRule, _anonymizationMethodTargetTypes.GetValueOrDefault(method));
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(expectedTargetDataType, result.TargetDataType);
        }

        [Theory]
        [MemberData(nameof(GetInvalidPathRules))]
        public void GivenAnInValidPathRule_WhenValidate_ResultShouldNotBeSuccess_AndErrorMessageShouldBeCorrect(string path, string method, string expectedErrorMessage)
        {
            var result = _fhirSchemaProvider.ValidateRule(path, method, AnonymizerRuleType.PathRule, _anonymizationMethodTargetTypes.GetValueOrDefault(method));
            Assert.False(result.Success);
            Assert.Equal(expectedErrorMessage, result.ErrorMessage);
        }
    }
}
