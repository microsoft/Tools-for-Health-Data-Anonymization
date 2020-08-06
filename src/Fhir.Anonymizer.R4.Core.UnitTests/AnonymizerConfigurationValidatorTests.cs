using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Health.Fhir23.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir23.Anonymizer.Core.Extensions;
using Newtonsoft.Json;
using Hl7.FhirPath;
using Xunit;

namespace Microsoft.Health.Fhir23.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizerConfigurationValidatorTests
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();

        public AnonymizerConfigurationValidatorTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public static IEnumerable<object[]> GetConfigsWithInvalidFhirVersion()
        {      
            yield return new object[] { "./TestConfigurationsVersion/configuration-Stu3-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-invalid-version.json" };
        }

        public static IEnumerable<object[]> GetConfigsWithValidFhirVersion()
        {
            yield return new object[] { "./TestConfigurationsVersion/configuration-R4-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-empty-version.json" };
            yield return new object[] { "./TestConfigurationsVersion/configuration-null-version.json" };
        }

        [Theory]
        [MemberData(nameof(GetConfigsWithInvalidFhirVersion))]
        public void GivenAnInvalidVersion_WhenCreateAnonymizerConfigurationManager_ExceptionShouldBeThrown(string configFilePath)
        {
            var content = File.ReadAllText(configFilePath);
            var _config = JsonConvert.DeserializeObject<AnonymizerConfiguration>(content);
            Assert.Throws<AnonymizerConfigurationErrorsException>(() => _validator.Validate(_config));
        }

        [Theory]
        [MemberData(nameof(GetConfigsWithValidFhirVersion))]
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
