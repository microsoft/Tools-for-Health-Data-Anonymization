using System.Collections.Generic;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizationFhirPathRuleTests
    {
        public static IEnumerable<object[]> GetFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address" }, { "method", "Test" } }, "Patient", "address", "Patient.address", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.nodesByType('address')" }, { "method", "Test" } }, "Patient", "nodesByType('address')", "Patient.nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "nodesByType('address')" }, { "method", "Test" } }, "", "nodesByType('address')", "nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient" }, { "method", "Test" } }, "Patient", "Patient", "Patient", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.abc.func(n=1).a.test('abc')" }, { "method", "Test" } }, "Patient", "abc.func(n=1).a.test('abc')", "Patient.abc.func(n=1).a.test('abc')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Resource" }, { "method", "Test" } }, "Resource", "Resource", "Resource", "Test" };
        }

        [Theory]
        [MemberData(nameof(GetFhirRuleConfigs))]
        public void GivenAFhirPath_WhenCreatePathRule_FhirRuleShouldBeCreateCorrectly(Dictionary<string, object> config, string expectResourceType, string expectExpression, string expectPath, string expectMethod)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);

            Assert.Equal(expectPath, rule.Path);
            Assert.Equal(expectMethod, rule.Method);
            Assert.Equal(expectResourceType, rule.ResourceType);
            Assert.Equal(expectExpression, rule.Expression);
        }
    }
}
