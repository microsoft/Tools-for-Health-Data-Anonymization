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
            Assert.Equal("http://127.0.0.1:5001", configuration.ParameterConfiguration.PresidioAnonymizerUrl);
            Assert.Equal("http://127.0.0.1:5002", configuration.ParameterConfiguration.PresidioAnalyzerUrl);
            Assert.Equal("en", configuration.ParameterConfiguration.PresidioAnalyzedLanguage);

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
                    DateShiftKey = "123",
                    PresidioAnonymizerUrl = "http://127.0.0.1:3000",
                    PresidioAnalyzerUrl =  "http://127.0.0.1:3001",
                    PresidioAnalyzedLanguage =  "es"
                }
            };

            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.Equal("123", configuration.ParameterConfiguration.DateShiftKey);
            Assert.Equal("http://127.0.0.1:3000", configuration.ParameterConfiguration.PresidioAnonymizerUrl);
            Assert.Equal("http://127.0.0.1:3001", configuration.ParameterConfiguration.PresidioAnalyzerUrl);
            Assert.Equal("es", configuration.ParameterConfiguration.PresidioAnalyzedLanguage);
        }
    }
}
