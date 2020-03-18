using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.ElementModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fhir.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();
        private readonly AnonymizerConfiguration _configuration;
        private readonly Dictionary<string, IEnumerable<AnonymizerRule>> _resourcePathRules;
        private readonly IEnumerable<AnonymizerRule> _genericPathRules;
        private readonly IEnumerable<AnonymizerRule> _typeRules;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _validator.Validate(configuration);
            configuration.GenerateDefaultParametersIfNotConfigured();
            _configuration = configuration;

            if (_configuration.PathRules != null)
            {
                _resourcePathRules = _configuration.PathRules.Where(entry => IsResourcePathRule(entry.Key))
                    .GroupBy(entry => ExtractResourceTypeFromPath(entry.Key))
                    .ToDictionary(group => group.Key, group => group.Select(item => new AnonymizerRule(item.Key, item.Value, AnonymizerRuleType.PathRule, item.Key)));
                _genericPathRules = _configuration.PathRules.Where(entry => !string.IsNullOrEmpty(entry.Key) && !IsResourcePathRule(entry.Key))
                    .Select(entry => new AnonymizerRule(entry.Key, entry.Value, AnonymizerRuleType.PathRule, entry.Key));
            }
            else
            {
                _resourcePathRules = new Dictionary<string, IEnumerable<AnonymizerRule>>();
                _genericPathRules = new List<AnonymizerRule>();
            }

            if (_configuration.TypeRules != null)
            {
                _typeRules = _configuration.TypeRules.Select(entry => new AnonymizerRule(entry.Key, entry.Value, AnonymizerRuleType.TypeRule, entry.Key));
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
            var allPathRules = new List<AnonymizerRule>(_genericPathRules);
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
