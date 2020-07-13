using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ConfigurationVersionTests
    {
        public ConfigurationVersionTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public static IEnumerable<object[]> GetInvalidVersionOfConfigs()
        {
        
            yield return new object[] { "./TestConfigurationsVersion/configuration-R4-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-empty-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-null-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-invalid-version.json" };

        }

        public static IEnumerable<object[]> GetValidVersionOfConfigs()
        {
            yield return new object[] { "./TestConfigurationsVersion/configuration-Stu3-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-both-version.json" };
        }

        [Theory]
        [MemberData(nameof(GetInvalidVersionOfConfigs))]
        public void GivenAnInvalidVersion_WhenCreateAnonymizerConfigurationManager_ExceptionShouldBeThrown(string configFilePath)
        {
            Assert.Throws<AnonymizerConfigurationErrorsException>(() => AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath));
        }

        [Theory]
        [MemberData(nameof(GetValidVersionOfConfigs))]
        public void GivenAValidVersion_WhenCreateAnonymizerConfigurationManager_ConfigurationShouldBeLoaded(string configFilePath)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var fhirRules = configurationManager.FhirPathRules;
            Assert.True(fhirRules.Any());
            fhirRules = configurationManager.FhirPathRules;
            Assert.Single(configurationManager.FhirPathRules.Where(r => "Patient".Equals(r.ResourceType)));
            Assert.Single(configurationManager.FhirPathRules.Where(r => "TestResource".Equals(r.ResourceType)));
            Assert.Single(configurationManager.FhirPathRules.Where(r => string.IsNullOrEmpty(r.ResourceType)));
            Assert.Single(configurationManager.FhirPathRules.Where(r => "Resource".Equals(r.ResourceType)));
            Assert.Single(configurationManager.FhirPathRules.Where(r => "Device".Equals(r.ResourceType)));

            var parameters = configurationManager.GetParameterConfiguration();
            Assert.True(!string.IsNullOrEmpty(parameters.DateShiftKey));
        }

    }
}
