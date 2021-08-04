using System.Collections.Generic;
using System.Linq;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizerConfigurationManagerTests
    {
        public AnonymizerConfigurationManagerTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public static IEnumerable<object[]> GetInvalidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-miss-rules.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-method.json" };
            yield return new object[] { "./TestConfigurations/configuration-invalid-fhirpath.json" };
        }

        public static IEnumerable<object[]> GetValidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-test-sample.json" };
        }

        [Theory]
        [MemberData(nameof(GetInvalidConfigs))]
        public void GivenAnInvalidConfig_WhenCreateAnonymizerConfigurationManager_ExceptionShouldBeThrown(string configFilePath)
        {
            Assert.Throws<AnonymizerConfigurationException>(() => AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath));
        }

        [Theory]
        [MemberData(nameof(GetValidConfigs))]
        public void GivenAValidConfig_WhenCreateAnonymizerConfigurationManager_ConfigurationShouldBeLoaded(string configFilePath)
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
            Assert.Equal(ProcessingErrorsOption.Raise, configurationManager.Configuration.processingErrors);
        }

        [Theory]
        [InlineData("abc123")]
        [InlineData("foldername")]
        [InlineData("filename")]
        public void GivenADateShiftKeyPrefix_WhenSet_DateShiftKeyPrefixShouldBeSetCorrectly(string dateShiftKeyPrefix)
        {
            var configFilePath = "./TestConfigurations/configuration-test-sample.json";
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            configurationManager.SetDateShiftKeyPrefix(dateShiftKeyPrefix);

            Assert.Equal(dateShiftKeyPrefix, configurationManager.GetParameterConfiguration().DateShiftKeyPrefix);
        }

        [Fact]
        public void GivenAConfigWithoutProcessingErrorsField_WhenCreateAnonymizerConfigurationManager_TheFieldShouldBeDefaultAsRaise()
        {
            var configFilePath = "./TestConfigurations/configuration-without-processing-error.json";
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            Assert.Equal(ProcessingErrorsOption.Raise, configurationManager.Configuration.processingErrors);
        }
    }
}
