using System;
using System.Collections.Generic;
using System.Linq;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation
{
    public class AnonymizerConfigurationValidator
    {
        private readonly FhirSchemaProvider _fhirSchemaProvider = new FhirSchemaProvider();
        private readonly Dictionary<string, HashSet<string>> _anonymizationMethodTargetTypes;

        public AnonymizerConfigurationValidator()
        {
            _anonymizationMethodTargetTypes = new Dictionary<string, HashSet<string>>();
            var anonymizationMethodNames = Enum.GetNames(typeof(AnonymizerMethod)).Select(name => name.ToLower());
            foreach (string methodName in anonymizationMethodNames)
            {
                if (string.Equals(methodName, "dateshift", StringComparison.InvariantCultureIgnoreCase))
                {
                    _anonymizationMethodTargetTypes.Add(methodName,
                        new HashSet<string> { "date", "dateTime", "instant" });
                }
                else
                {
                    _anonymizationMethodTargetTypes.Add(methodName, _fhirSchemaProvider.GetFhirAllTypes());
                }
            }
        }

        public void Validate(AnonymizerConfiguration config)
        {
            if (config.TypeRules == null && config.PathRules == null)
            {
                throw new AnonymizerConfigurationErrorsException("The configuration is invalid, please specify any pathRules or typeRules");
            }

            var invalidPathRuleCount = GetInvalidPathRuleCount(config);
            var invalidTypeRuleCount = GetInvalidTypeRuleCount(config);
            
            if (invalidPathRuleCount > 0 || invalidTypeRuleCount > 0)
            {
                throw new AnonymizerConfigurationErrorsException($"Configuration file validation failed, found {invalidPathRuleCount} invalid path rules and {invalidTypeRuleCount} invalid type rules.");
            }
        }

        private int GetInvalidPathRuleCount(AnonymizerConfiguration config)
        {
            var invalidPathRuleCount = 0;
            if (config.PathRules != null)
            {
                foreach (var rule in config.PathRules)
                {
                    var validationResult = _fhirSchemaProvider.ValidateRule(rule.Key, rule.Value, AnonymizerRuleType.PathRule, _anonymizationMethodTargetTypes.GetValueOrDefault(rule.Value));
                    if (!validationResult.Success)
                    {
                        invalidPathRuleCount++;
                        Console.Error.WriteLine($"Validate rule [{rule.Key}:{rule.Value}] failed: {validationResult.ErrorMessage}");
                    }
                }
            }

            return invalidPathRuleCount;
        }

        private int GetInvalidTypeRuleCount(AnonymizerConfiguration config)
        {
            var invalidTypeRuleCount = 0;
            if (config.TypeRules != null)
            {
                foreach (var rule in config.TypeRules)
                {
                    var validationResult = _fhirSchemaProvider.ValidateRule(rule.Key, rule.Value, AnonymizerRuleType.TypeRule, _anonymizationMethodTargetTypes.GetValueOrDefault(rule.Value));
                    if (!validationResult.Success)
                    {
                        invalidTypeRuleCount++;
                        Console.Error.WriteLine($"Validate rule [{rule.Key}:{rule.Value}] failed: {validationResult.ErrorMessage}");
                    }
                }
            }

            return invalidTypeRuleCount;
        }
    }
}
