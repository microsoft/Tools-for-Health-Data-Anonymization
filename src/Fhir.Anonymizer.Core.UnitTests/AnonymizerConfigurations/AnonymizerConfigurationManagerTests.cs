using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizerConfigurationManagerTests
    {
        public static IEnumerable<object[]> GetInvalidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-miss-rules.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-path.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-type.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-method.json" };
        }

        public static IEnumerable<object[]> GetValidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-test-sample.json" };
        }

        [Theory]
        [MemberData(nameof(GetInvalidConfigs))]
        public void GivenAnInvalidConfig_WhenCreateAnonymizerConfigurationManager_ExceptionShouldBeThrown(string configFilePath)
        {
            Assert.Throws<AnonymizerConfigurationErrorsException>(() => AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath));
        }

        [Theory]
        [MemberData(nameof(GetValidConfigs))]
        public void GivenAValidConfig_WhenCreateAnonymizerConfigurationManager_ConfigurationShouldBeLoaded(string configFilePath)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var patientRules = configurationManager.GetPathRulesByResourceType("Patient");
            Assert.True(patientRules.Any());
            var typeRules = configurationManager.GetTypeRules();
            Assert.True(typeRules.Any());
            var parameters = configurationManager.GetParameterConfiguration();
            Assert.True(!string.IsNullOrEmpty(parameters.DateShiftKey));
        }

        [Theory]
        [InlineData("abc123")]
        [InlineData("foldername")]
        [InlineData("filename")]
        public void GivenADateShiftPrefix_WhenSet_DateShiftPrefixShouldBeSetCorrectly(string dateShiftPrefix)
        {
            var configFilePath = "./TestConfigurations/configuration-test-sample.json";
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            configurationManager.SetDateShiftPrefix(dateShiftPrefix);

            Assert.Equal(dateShiftPrefix, configurationManager.GetParameterConfiguration().DateShiftPrefix);
        }
    }
}
