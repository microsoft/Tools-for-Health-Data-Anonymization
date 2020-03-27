using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fhir.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();
        private readonly AnonymizerConfiguration _configuration;
        private readonly Dictionary<string, IEnumerable<AnonymizerRule>> _resourcePathRules;
        private readonly IEnumerable<AnonymizerRule> _typeRules;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _validator.Validate(configuration);
            configuration.GenerateDefaultParametersIfNotConfigured();
            _configuration = configuration;

            if (_configuration.PathRules != null)
            {
                _resourcePathRules = _configuration.PathRules.GroupBy(entry => ExtractResourceTypeFromPath(entry.Key))
                    .ToDictionary(group => group.Key, group => group.Select((item, index) => new AnonymizerRule(item.Key, item.Value, AnonymizerRuleType.PathRule, item.Key, index)));
            }
            else
            {
                _resourcePathRules = new Dictionary<string, IEnumerable<AnonymizerRule>>();
            }

            if (_configuration.TypeRules != null)
            {
                _typeRules = _configuration.TypeRules.Keys.Select((key, index) => new AnonymizerRule(key, _configuration.TypeRules[key], AnonymizerRuleType.TypeRule, key, index));
            }
            else
            {
                _typeRules = new List<AnonymizerRule>();
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
            var allPathRules = new List<AnonymizerRule>();
            if (!string.IsNullOrEmpty(resourceType) && _resourcePathRules.ContainsKey(resourceType))
            {
                allPathRules.AddRange(_resourcePathRules[resourceType]);
            }

            return allPathRules;
        }

        public IEnumerable<AnonymizerRule> GetTypeRules()
        {
            return _typeRules;
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
