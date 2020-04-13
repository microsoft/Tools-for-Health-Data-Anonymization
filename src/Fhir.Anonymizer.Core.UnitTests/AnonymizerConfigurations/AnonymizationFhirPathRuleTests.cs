using System;
using System.Collections.Generic;
using System.Text;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizationFhirPathRuleTests
    {
        public static IEnumerable<object[]> GetFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, string>() { { "path", "Patient.address" }, { "method", "Test" } }, "Patient", "address", "Patient.address", "Test" };
            yield return new object[] { new Dictionary<string, string>() { { "path", "Patient.nodesByType('address')" }, { "method", "Test" } }, "Patient", "nodesByType('address')", "Patient.nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, string>() { { "path", "nodesByType('address')" }, { "method", "Test" } }, "", "nodesByType('address')", "nodesByType('address')", "Test" };
        }

        [Theory]
        [MemberData(nameof(GetFhirRuleConfigs))]
        public void GivenAFhirPath_WhenCreatePathRule_FhirRuleShouldBeCreateCorrectly(Dictionary<string, string> config, string expectResourceType, string expectExpression, string expectPath, string expectMethod)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);

            Assert.Equal(expectPath, rule.Path);
            Assert.Equal(expectMethod, rule.Method);
            Assert.Equal(expectResourceType, rule.ResourceType);
            Assert.Equal(expectExpression, rule.Expression);
        }
    }
}
