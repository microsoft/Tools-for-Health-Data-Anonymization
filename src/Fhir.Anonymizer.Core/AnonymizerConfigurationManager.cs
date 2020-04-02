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
        private readonly Dictionary<string, IEnumerable<AnonymizerRule>> _resourcePathRules;

        public AnonymizerRule[] FhirPathRules { get; private set; } = null;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _validator.Validate(configuration);
            configuration.GenerateDefaultParametersIfNotConfigured();

            _configuration = configuration;

            if (configuration.FhirPathRules!= null)
            {
                //TODO add capability check here.

                FhirPathRules = _configuration.FhirPathRules.Select(entry => new AnonymizerRule(entry.Key, entry.Value, AnonymizerRuleType.FhirPathRule, entry.Key)).ToArray();
            }
            else
            {
                if (_configuration.PathRules != null)
                {
                    _resourcePathRules = _configuration.PathRules.Where(entry => IsResourcePathRule(entry.Key))
                        .GroupBy(entry => ExtractResourceTypeFromPath(entry.Key))
                        .ToDictionary(group => group.Key, group => group.Select(item => new AnonymizerRule(item.Key, item.Value, AnonymizerRuleType.PathRule, item.Key)));
                }
                else
                {
                    _resourcePathRules = new Dictionary<string, IEnumerable<AnonymizerRule>>();
                }
            }
        }

        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string configFilePath)
        {
            try
            {
                var content = File.ReadAllText(configFilePath);

                JsonLoadSettings settings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var token = JToken.Parse(content, settings);
                var configuration = token.ToObject<AnonymizerConfiguration>();
                return new AnonymizerConfigurationManager(configuration);
            }
            catch (IOException innerException)
            {
                throw new IOException($"Failed to read configuration file {configFilePath}", innerException);
            }
            catch (JsonException innerException)
            {
                throw new JsonException($"Failed to parse configuration file {configFilePath}", innerException);
            }
        }

        public IEnumerable<AnonymizerRule> GetPathRulesByResourceType(string resourceType)
        {
            if (string.IsNullOrEmpty(resourceType) || !_resourcePathRules.ContainsKey(resourceType))
            {
                return new List<AnonymizerRule>();
            }
            return _resourcePathRules[resourceType];
        }

        public Dictionary<string, string> GetTypeRules()
        {
            return _configuration.TypeRules;
        }

        public ParameterConfiguration GetParameterConfiguration()
        {
            return _configuration.ParameterConfiguration;
        }

        private bool IsResourcePathRule(string path)
        {
            return !string.IsNullOrEmpty(path) && char.IsUpper(path.First());
        }

        private string ExtractResourceTypeFromPath(string path)
        {
            var dotIndex = path.IndexOf('.');
            return dotIndex == -1 ? string.Empty : path.Substring(0, dotIndex);
        }

    }
}
