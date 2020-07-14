using System.Collections.Generic;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace Fhir.Anonymizer.Shared.Core.UnitTest.Processors.Settings
{
    public class SubstituteSettingTests
    {
        public static IEnumerable<object[]> GetSubstituteFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", null } }, null };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", string.Empty } }, string.Empty };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", "abc" } }, "abc" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address.city" }, { "method", "substitute" }, { "replaceWith", "**^^©®ÄÄÄÄ" } }, "**^^©®ÄÄÄÄ" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address" }, { "method", "substitute" }, { "replaceWith", "{}" } }, "{}" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address" }, { "method", "substitute" }, { "replaceWith", "{\"city\":\"abc\"}" } }, "{\"city\":\"abc\"}" };
        }

        [Theory]
        [MemberData(nameof(GetSubstituteFhirRuleConfigs))]
        public void GivenASubstituteRule_WhenCreate_ReplacementValueShouldBeParsedCorrectly(Dictionary<string, object> config, string expectedValue)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);
            Assert.NotEmpty(rule.RuleSettings);

            var substituteSetting = SubstituteSetting.CreateFromRuleSettings(rule.RuleSettings);
            Assert.Equal(expectedValue, substituteSetting.ReplaceWith);
        }
    }
}