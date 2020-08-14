using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Settings
{
    public class PerturbSettingTests
    {
        public static IEnumerable<object[]> GetPerturbFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", 1 } }, 1, 2, PerturbRangeType.Fixed };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", "1" }, { "roundTo", 28 } }, 1, 28, PerturbRangeType.Fixed };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", 0.1 }, { "rangeType", "Proportional" } }, 0.1, 2, PerturbRangeType.Proportional };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", 0.1 }, { "roundTo", 10 }, { "rangeType", "Proportional" } }, 0.1, 10, PerturbRangeType.Proportional };
        }

        public static IEnumerable<object[]> GetInvalidPerturbFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", "test" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", "-1" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", "123" }, { "roundTo", "abc" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", 1 }, { "roundTo", -200 } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", 1 }, { "roundTo", 29 } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "span", 0.1 }, { "rangeType", "Proportionaal" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Condition.onset" }, { "method", "perturb" }, { "roundTo", 10 }, { "rangeType", "Proportional" } } };
        }

        [Theory]
        [MemberData(nameof(GetPerturbFhirRuleConfigs))]
        public void GivenAPerturbSetting_WhenCreate_SettingPropertiesShouldBeParsedCorrectly(Dictionary<string, object> config, double expectedSpan, double expectedRoundTo, PerturbRangeType expectedRangeType)
        {
            var perturbSetting = PerturbSetting.CreateFromRuleSettings(config);
            Assert.Equal(expectedSpan, perturbSetting.Span);
            Assert.Equal(expectedRoundTo, perturbSetting.RoundTo);
            Assert.Equal(expectedRangeType, perturbSetting.RangeType);
        }

        [Theory]
        [MemberData(nameof(GetInvalidPerturbFhirRuleConfigs))]
        public void GivenAInvalidPerturbSetting_WhenValidate_ExceptionShouldBeThrown(Dictionary<string, object> config)
        {
            Assert.Throws<AnonymizerConfigurationErrorsException>(() => PerturbSetting.ValidateRuleSettings(config));
        }
    }
}