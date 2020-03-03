using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ResourceAnonymizerContextTests
    {
        [Fact]
        public void GivenATypeRule_WhenParseRule_TransformedPathRuleShouldBeReturned()
        {
            AnonymizerConfiguration configuration = new AnonymizerConfiguration()
            {
                PathRules = new Dictionary<string, string>(),
                ParameterConfiguration = new ParameterConfiguration(),
                TypeRules = new Dictionary<string, string>()
                {
                    { "Address", "redact"},
                    { "HumanName", "redact" },
                    { "dateTime", "dateShift"}
                }
            };

            AnonymizerConfigurationManager configurationManager = new AnonymizerConfigurationManager(configuration);
            var context = ResourceAnonymizerContext.Create(TestPatientElementNode(), configurationManager);

            Assert.Contains("Patient.address", context.PathSet);
            Assert.Contains("Patient.name", context.PathSet);
            Assert.Contains("Patient.address.period.start", context.PathSet);
            Assert.Contains("Patient.identifier.period.start", context.PathSet);
        }

        [Fact]
        public void GivenConflictTypeRuleAndPathRule_WhenParseRule_TypeRuleShouldBeIgnored()
        {
            AnonymizerConfiguration configuration = new AnonymizerConfiguration()
            {
                PathRules = new Dictionary<string, string>()
                {
                    { "Patient.address", "keep"}
                },
                ParameterConfiguration = new ParameterConfiguration(),
                TypeRules = new Dictionary<string, string>()
                {
                    { "Address", "redact"}
                }
            };

            AnonymizerConfigurationManager configurationManager = new AnonymizerConfigurationManager(configuration);
            var context = ResourceAnonymizerContext.Create(TestPatientElementNode(), configurationManager);

            Assert.Contains("Patient.address", context.PathSet);
            Assert.Equal("keep", context.RuleList.First().Method);
        }

        [Fact]
        public void GivenATypeRuleContainsAnother_WhenParseRule_NestedOneShouldOverwriteInheritedOne()
        {
            AnonymizerConfiguration configuration = new AnonymizerConfiguration()
            {
                PathRules = new Dictionary<string, string>(),
                ParameterConfiguration = new ParameterConfiguration(),
                TypeRules = new Dictionary<string, string>()
                {
                    { "Address", "redact"},
                    { "dateTime", "keep"}
                }
            };

            AnonymizerConfigurationManager configurationManager = new AnonymizerConfigurationManager(configuration);
            var context = ResourceAnonymizerContext.Create(TestPatientElementNode(), configurationManager);

            Assert.Contains("Patient.address", context.PathSet);
            Assert.Contains("Patient.address.period.start", context.PathSet);
            Assert.Equal("keep", context.RuleList.First(r => r.Path.Equals("Patient.address.period.start")).Method);
            Assert.Equal("redact", context.RuleList.First(r => r.Path.Equals("Patient.address")).Method);
        }

        private static ElementNode TestPatientElementNode()
        {
            var parser = new FhirJsonParser();
            return ElementNode.FromElement(parser.Parse(TestPatientSample).ToTypedElement());
        }

        private const string TestPatientSample =
@"
{
  ""resourceType"": ""Patient"",
  ""id"": ""example"",
  ""identifier"": [
    {
      ""type"": {
        ""coding"": [
          {
            ""system"": ""http://terminology.hl7.org/CodeSystem/v2-0203"",
            ""code"": ""MR""
          }
        ]
      },
      ""period"": {
        ""start"": ""2001-05-06""
      },
      ""system"": ""urn:oid:1.2.36.146.595.217.0.1""
    }
  ],
  ""active"": true,
  ""name"": [
    {
      ""use"": ""official"",
      ""family"": ""Chalmers"",
      ""given"": [
        ""Peter"",
        ""James""
      ]
    }
  ],
  ""address"": [
    {
      ""use"": ""home"",
      ""type"": ""both"",
      ""line"": [
        ""534 Erewhon St""
      ],
      ""city"": ""PleasantVille"",
      ""district"": ""Rainbow"",
      ""state"": ""Vic"",
      ""postalCode"": ""3999"",
      ""period"": {
        ""start"": ""1974-12-25""
      }
    }
  ],
  ""managingOrganization"": {
    ""reference"": ""Organization/1""
  }
}
";
    }
}
