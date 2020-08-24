using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class GeneralizeSetting
    {
        public JObject Cases { get; set; }
        public GeneralizationOtherValuesOperation OtherValues { get; set; }

        public static GeneralizeSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            JObject cases = null;
            GeneralizationOtherValuesOperation otherValues = GeneralizationOtherValuesOperation.redact;
            if (ruleSettings.ContainsKey(RuleKeys.Cases))
            {
                try
                {
                    cases = JObject.Parse(ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString());
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationErrorsException($"Invalid cases {RuleKeys.Cases}", ex);
                }  
            }

            if (ruleSettings.ContainsKey(RuleKeys.OtherValues))
            {
                Enum.TryParse(ruleSettings.GetValueOrDefault(RuleKeys.OtherValues)?.ToString(), true, out otherValues);
            }

            return new GeneralizeSetting
            {
                OtherValues = otherValues,
                Cases = cases
            };
        }

        public static void ValidateRuleSettings(Dictionary<string, object> ruleSettings)
        {
            FhirPathCompiler compiler = new FhirPathCompiler();
            if (ruleSettings == null)
            {
                throw new AnonymizerConfigurationErrorsException("Generalize rule should not be null.");
            }

            if (!ruleSettings.ContainsKey(Constants.PathKey))
            {
                throw new AnonymizerConfigurationErrorsException("Missing path in FHIR path rule config.");
            }

            if (!ruleSettings.ContainsKey(Constants.MethodKey))
            {
                throw new AnonymizerConfigurationErrorsException("Missing method in FHIR path rule config.");
            }

            if (!ruleSettings.ContainsKey(RuleKeys.Cases))
            {
                throw new AnonymizerConfigurationErrorsException("Missing cases in FHIR path rule config.");
            }

            try
            {
                JObject Cases = JObject.Parse(ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString());
                foreach (var eachCase in Cases)
                {
                    compiler.Compile(eachCase.Key.ToString());
                    compiler.Compile(eachCase.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationErrorsException($"Invalid cases expression {ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString()}", ex);
            }           

            var supportedOtherValuesOperations = Enum.GetNames(typeof(GeneralizationOtherValuesOperation)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            if (ruleSettings.ContainsKey(RuleKeys.OtherValues) && !supportedOtherValuesOperations.Contains(ruleSettings[RuleKeys.OtherValues].ToString()))
            {
                throw new AnonymizerConfigurationErrorsException($"OtherValues setting is invalid at {ruleSettings[RuleKeys.OtherValues]}.");
            }
        }
    }
}