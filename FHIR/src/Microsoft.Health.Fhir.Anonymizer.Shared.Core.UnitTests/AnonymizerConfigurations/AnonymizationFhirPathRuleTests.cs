using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizationFhirPathRuleTests
    {
        public static IEnumerable<object[]> GetFhirRuleConfigs()
        {
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.address" }, { "method", "Test" } }, "Patient", "address", "Patient.address", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.nodesByType('address')" }, { "method", "Test" } }, "Patient", "nodesByType('address')", "Patient.nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "nodesByType('address')" }, { "method", "Test" } }, "", "nodesByType('address')", "nodesByType('address')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "nodesByName('telecom')" }, { "method", "Test" } }, "", "nodesByName('telecom')", "nodesByName('telecom')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient" }, { "method", "Test" } }, "Patient", "Patient", "Patient", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Patient.abc.func(n=1).a.test('abc')" }, { "method", "Test" } }, "Patient", "abc.func(n=1).a.test('abc')", "Patient.abc.func(n=1).a.test('abc')", "Test" };
            yield return new object[] { new Dictionary<string, object>() { { "path", "Resource" }, { "method", "Test" } }, "Resource", "Resource", "Resource", "Test" };
        }

        public static IEnumerable<object[]> GetNodesByType()
        {
            yield return new object[] { "nodesByType('Address')", "Address", "" };
            yield return new object[] { "nodesByType(\"Address\")", "Address", "" };
            yield return new object[] { "nodesByType('Address').country", "Address", "country" };
            yield return new object[] { "nodesByType(\"Address\").country", "Address", "country" };
        }

        public static IEnumerable<object[]> GetNodesByTypeNotMatched()
        {
            // Rules that are invalid
            yield return new object[] { "nodesByType(" };
            yield return new object[] { "nodesByType()" };
            yield return new object[] { "nodesByType()." };
            yield return new object[] { ".nodesByType()" };
            yield return new object[] { "nodesByType('Address)" };
            yield return new object[] { "nodesByType('???')" };
            yield return new object[] { "nodesByType('Address').???" };
            yield return new object[] { "nodesByType('Address')..." };

            // Rules that have resource types
            yield return new object[] { "Patient.nodesByType('Address')" };
            yield return new object[] { "Resource.nodesByType('Address')" };

            // Rules that have complex expressions
            yield return new object[] { "nodesByType('Address').where(city='testcity')" };
            yield return new object[] { "nodesByType('Address').where(city='testcity').city" };
            yield return new object[] { "nodesByType('Address').nodesByType('Period')" };
            yield return new object[] { "nodesByType('Address').nodesByName('value')" };

            // Rules that both have resource types and complex expressions
            yield return new object[] { "Patient.nodesByType('Address').where(city='testcity').city" };
        }

        public static IEnumerable<object[]> GetNodesByName()
        {
            yield return new object[] { "nodesByName('telecom')", "telecom", "" };
            yield return new object[] { "nodesByName(\"telecom\")", "telecom", "" };
            yield return new object[] { "nodesByName('telecom').value", "telecom", "value" };
            yield return new object[] { "nodesByName(\"telecom\").value", "telecom", "value" };
        }

        public static IEnumerable<object[]> GetNodesByNameNotMatched()
        {
            // Rules that are invalid
            yield return new object[] { "nodesByName(" };
            yield return new object[] { "nodesByName()" };
            yield return new object[] { "nodesByName()." };
            yield return new object[] { ".nodesByName()" };
            yield return new object[] { "nodesByName('telecom)" };
            yield return new object[] { "nodesByName('???')" };
            yield return new object[] { "nodesByName('telecom').???" };
            yield return new object[] { "nodesByName('telecom')..." };

            // Rules that have resource types
            yield return new object[] { "Patient.nodesByName('telecom')" };
            yield return new object[] { "Resource.nodesByName('telecom')" };

            // Rules that have complex expressions
            yield return new object[] { "nodesByName('telecom').where(value='testvalue')" };
            yield return new object[] { "nodesByName('telecom').where(value='testvalue').value" };
            yield return new object[] { "nodesByName('telecom').nodesByType('Period')" };
            yield return new object[] { "nodesByName('telecom').nodesByName('value')" };

            // Rules that both have resource types and complex expressions
            yield return new object[] { "Patient.nodesByName('Address').where(value='testvalue').value" };
        }

        [Theory]
        [MemberData(nameof(GetFhirRuleConfigs))]
        public void GivenAFhirPath_WhenCreatePathRule_FhirPathRuleShouldBeCreatedCorrectly(Dictionary<string, object> config, string expectResourceType, string expectExpression, string expectPath, string expectMethod)
        {
            var rule = AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(config);

            Assert.Equal(expectPath, rule.Path);
            Assert.Equal(expectMethod, rule.Method);
            Assert.Equal(expectResourceType, rule.ResourceType);
            Assert.Equal(expectExpression, rule.Expression);
        }

        [Theory]
        [MemberData(nameof(GetNodesByType))]
        public void GivenNodesByTypeRule_WhenMatch_ComponentsShouldBeMatchedCorrectly(string rule, string expectType, string expectExpression)
        {
            var match = AnonymizationFhirPathRule.TypeRuleRegex.Match(rule);
            Assert.True(match.Success);
            Assert.Equal(expectType, match.Groups["type"].Value);
            Assert.Equal(expectExpression, match.Groups["expression"].Value);
        }

        [Theory]
        [MemberData(nameof(GetNodesByTypeNotMatched))]
        public void GivenNodesByTypeRuleNotMatched_WhenMatch_ComponentsShouldNotBeMatched(string rule)
        {
            var match = AnonymizationFhirPathRule.TypeRuleRegex.Match(rule);
            Assert.False(match.Success);
        }

        [Theory]
        [MemberData(nameof(GetNodesByName))]
        public void GivenNodesByNameRule_WhenMatch_ComponentsShouldBeMatchedCorrectly(string rule, string expectName, string expectExpression)
        {
            var match = AnonymizationFhirPathRule.NameRuleRegex.Match(rule);
            Assert.True(match.Success);
            Assert.Equal(expectName, match.Groups["name"].Value);
            Assert.Equal(expectExpression, match.Groups["expression"].Value);
        }

        [Theory]
        [MemberData(nameof(GetNodesByNameNotMatched))]
        public void GivenNodesByNameRuleNotMatched_WhenMatch_ComponentsShouldNotBeMatched(string rule)
        {
            var match = AnonymizationFhirPathRule.NameRuleRegex.Match(rule);
            Assert.False(match.Success);
        }
    }
}
