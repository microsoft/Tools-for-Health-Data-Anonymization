using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Settings
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

        public static IEnumerable<object[]> GetInvalidSubstituteFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address.city" }, { "method", "substitute" } } };
        }

        [Theory]
        [MemberData(nameof(GetSubstituteFhirRuleConfigs))]
        public void GivenASubstituteSetting_WhenCreate_ReplacementValueShouldBeParsedCorrectly(Dictionary<string, object> config, string expectedValue)
        {
            var substituteSetting = SubstituteSetting.CreateFromRuleSettings(config);
            Assert.Equal(expectedValue, substituteSetting.ReplaceWith);
        }

        [Theory]
        [MemberData(nameof(GetInvalidSubstituteFhirRuleConfigs))]
        public void GivenAInvalidSubstituteSetting_WhenValidate_ExceptionShouldBeThrown(Dictionary<string, object> config)
        {
            Assert.Throws<AnonymizerConfigurationException>(() => SubstituteSetting.ValidateRuleSettings(config));
        }
    }
}