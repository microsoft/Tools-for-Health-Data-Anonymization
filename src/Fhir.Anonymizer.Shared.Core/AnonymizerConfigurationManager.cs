using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fhir.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();
        private readonly AnonymizerConfiguration _configuration;

        public AnonymizationFhirPathRule[] FhirPathRules { get; private set; } = null;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _validator.Validate(configuration);
            configuration.GenerateDefaultParametersIfNotConfigured();

            _configuration = configuration;

            FhirPathRules = _configuration.FhirPathRules.Select(entry => AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(entry)).ToArray();
        }

        public static AnonymizerConfigurationManager CreateFromSettingsInJson(string settingsInJson)
        {
            try
            {
                JsonLoadSettings settings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var token = JToken.Parse(settingsInJson, settings);
                var configuration = token.ToObject<AnonymizerConfiguration>();
                return new AnonymizerConfigurationManager(configuration);
            }
            catch (JsonException innerException)
            {
                throw new JsonException($"Failed to parse configuration file", innerException);
            }
        }

        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string configFilePath)
        {
            try
            {
                var content = File.ReadAllText(configFilePath);

                return CreateFromSettingsInJson(content);
            }
            catch (IOException innerException)
            {
                throw new IOException($"Failed to read configuration file {configFilePath}", innerException);
            }
        }

        public ParameterConfiguration GetParameterConfiguration()
        {
            return _configuration.ParameterConfiguration;
        }

        public void SetDateShiftKeyPrefix(string prefix)
        {
            _configuration.ParameterConfiguration.DateShiftKeyPrefix = prefix;
        }
    }
}
