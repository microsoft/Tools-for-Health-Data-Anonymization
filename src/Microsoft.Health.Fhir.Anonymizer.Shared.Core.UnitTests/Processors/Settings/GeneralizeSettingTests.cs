using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Settings
{
    public class GeneralizeSettingTests
    {
        public static IEnumerable<object[]> GetGeneralizeFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<=@2010-01-01 and $this>=@2010-01-01\": \"10\"}" } } , "{\"$this<=@2010-01-01 and $this>=@2010-01-01\": \"10\"}", "redact" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<=10 and $this>=0\": \"10\"}" }, { "otherValues", "keep" } }, "{\"$this<=10 and $this>=0\": \"10\"}", "keep" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<=10 and $this>=0\": \"10\"}" }, { "otherValues", "redact" } }, "{\"$this<=10 and $this>=0\": \"10\"}", "redact" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"\": \"\"}" }, { "otherValues", "redact" } }, "{\"\": \"\"}", "redact" };
        }

        public static IEnumerable<object[]> GetInvalidGeneralizeFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<=10 add $this>=0\": \"10\"}" } }};
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<=10 and $this>=0\": \"10 add\"}" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this sub 1\": \"10\"}" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<10\"\"\": \"10++\"}" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this<10\": \"10\"}" }, { "otherValues", "unknown" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"$this = @2015-01-01T00:00\": \"@2015-01-01T00:00:00Z\"}" }} };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "cases", "{\"\": \"\"}" }, { "otherValues", "redact" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" }, { "method", "generalize" }, { "otherValues", "keep" } } };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.birthDate" },  { "cases", "{\"$this<10\": \"10\"}" }, { "otherValues", "keep" } } };
            yield return new object[] { new Dictionary<string, object>() { { "method", "generalize" }, { "cases", "{\"$this<10\": \"10\"}" }, { "otherValues", "keep" } } };
            yield return new object[] { new Dictionary<string, object>() {  } };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(GetGeneralizeFhirRuleConfigs))]
        public void GivenAGeneralizeSetting_WhenCreate_SettingPropertiesShouldBeParsedCorrectly(Dictionary<string, object> config, string expectedCases, string expectedOtherValues)
        {
            var generalizeSetting = GeneralizeSetting.CreateFromRuleSettings(config);
            Assert.Equal(expectedCases, generalizeSetting.Cases);
            Assert.Equal(expectedOtherValues, generalizeSetting.OtherValues);
        }

        [Theory]
        [MemberData(nameof(GetInvalidGeneralizeFhirRuleConfigs))]
        public void GivenAInvalidGeneralizeSetting_WhenValidate_ExceptionShouldBeThrown(Dictionary<string, object> config)
        {
            Assert.Throws<AnonymizerConfigurationErrorsException>(() => GeneralizeSetting.ValidateRuleSettings(config));
        }
    }
}