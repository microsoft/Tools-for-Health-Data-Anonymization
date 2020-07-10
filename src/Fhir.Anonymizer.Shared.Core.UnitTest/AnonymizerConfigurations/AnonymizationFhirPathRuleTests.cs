using System;
using System.Collections.Generic;
using System.Text;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizationFhirPathRuleTests
    {
        public static IEnumerable<object[]> GetFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address" }, { "method", "Test" } }, "Patient", "address", "Patient.address", "Test" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.nodesByType('address')" }, { "method", "Test" } }, "Patient", "nodesByType('address')", "Patient.nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "nodesByType('address')" }, { "method", "Test" } }, "", "nodesByType('address')", "nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient" }, { "method", "Test" } }, "Patient", "Patient", "Patient", "Test" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.abc.func(n=1).a.test('abc')" }, { "method", "Test" } }, "Patient", "abc.func(n=1).a.test('abc')", "Patient.abc.func(n=1).a.test('abc')", "Test" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Resource" }, { "method", "Test" } }, "Resource", "Resource", "Resource", "Test" };
        }

        public static IEnumerable<object[]> GetPrimiteSubstituteFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", null } }, null };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", string.Empty } }, string.Empty };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", "abc" } }, "abc" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", "**^^©®ÄÄÄÄ" } }, "**^^©®ÄÄÄÄ" };
        }

        public static IEnumerable<object[]> GetComplexSubstituteFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address" }, { "method", "substitute" }, { "replaceWith", "{}" } }, "{}" };
            yield return new object[] { new Dictionary<string, JToken>() { { "path", "Patient.address" }, { "method", "substitute" }, { "replaceWith", "{\"city\":\"abc\"}"} }, "{\"city\":\"abc\"}" };
        }

        [Theory]
        [MemberData(nameof(GetFhirRuleConfigs))]
        public void GivenAFhirPath_WhenCreatePathRule_FhirRuleShouldBeCreateCorrectly(Dictionary<string, JToken> config, string expectResourceType, string expectExpression, string expectPath, string expectMethod)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);

            Assert.Equal(expectPath, rule.Path);
            Assert.Equal(expectMethod, rule.Method);
            Assert.Equal(expectResourceType, rule.ResourceType);
            Assert.Equal(expectExpression, rule.Expression);
        }

        [Theory]
        [MemberData(nameof(GetPrimiteSubstituteFhirRuleConfigs))]
        public void GivenAPrimitiveSubstituteRule_WhenCreatePathRule_ReplacementValueShouldBeParsedCorrectly(Dictionary<string, JToken> config, string expectedValue)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);

            Assert.True(rule.IsPrimitiveReplacement);
            Assert.Equal(expectedValue, rule.ReplaceWith);
        }

        [Theory]
        [MemberData(nameof(GetComplexSubstituteFhirRuleConfigs))]
        public void GivenAComplexSubstituteRule_WhenCreatePathRule_ReplacementValueShouldBeParsedCorrectly(Dictionary<string, JToken> config, string expectedValue)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);

            Assert.True(rule.IsPrimitiveReplacement);
            Assert.Equal(expectedValue, rule.ReplaceWith);
        }
    }
}
