using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizerConfigurationTests
    {
        [Fact]
        public void GivenAnEmptyConfig_GenerateDefaultParametersIfNotConfigured_DefaultValueShouldBeAdded()
        {
            var configuration = new AnonymizerConfiguration();
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.NotNull(configuration.ParameterConfiguration);
            Assert.Equal(32, configuration.ParameterConfiguration.DateShiftKey.Length);

            configuration = new AnonymizerConfiguration() { ParameterConfiguration = new ParameterConfiguration() };
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.NotNull(configuration.ParameterConfiguration);
            Assert.Equal(32, configuration.ParameterConfiguration.DateShiftKey.Length);
        }

        [Fact]
        public void GivenConfigWithParameter_GenerateDefaultParametersIfNotConfigured_ParametersShouldNotOverwrite()
        {
            var configuration = new AnonymizerConfiguration();
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.NotNull(configuration.ParameterConfiguration);
            Assert.Equal(32, configuration.ParameterConfiguration.DateShiftKey.Length);

            configuration = new AnonymizerConfiguration()
            {
                ParameterConfiguration = new ParameterConfiguration()
                {
                    DateShiftKey = "123"
                }
            };

            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.Equal("123", configuration.ParameterConfiguration.DateShiftKey);
        }
    }
}
