using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ResourceAnonymizerContextTests
    {
        [Fact]
        public void GivenConfiguration_WhenGetPathRuleForNode_CorrentPathRuleShouldBeReturned()
        {
            AnonymizerConfiguration configuration = new AnonymizerConfiguration()
            {
                PathRules = new Dictionary<string, string>()
                {
                    { "Patient.name.family", "redact"},
                },
                ParameterConfiguration = new ParameterConfiguration(),
                TypeRules = new Dictionary<string, string>()
            };

            AnonymizerConfigurationManager configurationManager = new AnonymizerConfigurationManager(configuration);
            var root = TestPatientElementNode();
            var context = ResourceAnonymizerContext.Create(root, configurationManager);
            var node = root.Select("Patient.name.family").Cast<ElementNode>().First();
            var rule = context.GetNodePathRule(node);
            Assert.Equal("redact", rule.Method);
        }

        [Fact]
        public void GivenATypeRuleEqualsToAnother_WhenGetTypeRuleForNode_FirstOneShouldOverwriteLaterOnes()
        {
            AnonymizerConfiguration configuration = new AnonymizerConfiguration()
            {
                PathRules = new Dictionary<string, string>(),
                ParameterConfiguration = new ParameterConfiguration(),
                TypeRules = new Dictionary<string, string>()
                {
                    { "Address.period.start", "redact"},
                    { "dateTime", "keep"},
                    { "Identifier.period.start", "redact"},
                    { "Period.start", "dateshift"}
                }
            };

            AnonymizerConfigurationManager configurationManager = new AnonymizerConfigurationManager(configuration);
            var root = TestPatientElementNode();
            var context = ResourceAnonymizerContext.Create(root, configurationManager);
            var node1 = root.Select("Patient.address.period.start").Cast<ElementNode>().First();
            var rule1 = context.GetNodeTypeRule(node1);
            Assert.Equal("redact", rule1.Method);
            
            var node2 = root.Select("Patient.identifier.period.start").Cast<ElementNode>().First();
            var rule2 = context.GetNodeTypeRule(node2);
            Assert.Equal("keep", rule2.Method);
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
