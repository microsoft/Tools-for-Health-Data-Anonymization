using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();
        private readonly AnonymizerConfiguration _configuration;

        public AnonymizationFhirPathRule[] FhirPathRules { get; private set; } = null;
        public AnonymizerConfiguration Configuration { get { return _configuration; } }

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
                throw new AnonymizerConfigurationException($"Failed to parse configuration file", innerException);
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
                throw new AnonymizerConfigurationException($"Failed to read configuration file {configFilePath}", innerException);
            }
        }

        /// <summary>
        /// Creates an AnonymizerConfigurationManager from an IConfiguration instance.
        /// This enables support for multiple configuration sources (files, environment variables, etc.).
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">Optional section name to bind from. If null, binds from root.</param>
        /// <returns>A new AnonymizerConfigurationManager instance.</returns>
        public static AnonymizerConfigurationManager CreateFromConfiguration(IConfiguration configuration, string sectionName = null)
        {
            var config = configuration.GetAnonymizerConfiguration(sectionName);
            return new AnonymizerConfigurationManager(config);
        }

        /// <summary>
        /// Creates an AnonymizerConfigurationManager from a configuration file using IConfiguration.
        /// This method supports the standard .NET configuration system.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file.</param>
        /// <returns>A new AnonymizerConfigurationManager instance.</returns>
        public static AnonymizerConfigurationManager CreateFromConfigurationFileWithIConfiguration(string configFilePath)
        {
            var config = Microsoft.Health.Fhir.Anonymizer.Core.Extensions.ConfigurationExtensions.CreateAnonymizerConfigurationFromFile(configFilePath);
            return new AnonymizerConfigurationManager(config);
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
