using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;

namespace Fhir.Anonymizer.Core.Processors.Settings
{
    public class GeneralizeSetting
    {
        public string Cases { get; set; }
        public string OtherValues { get; set; }

        public static GeneralizeSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            string cases = null;
            string otherValues = "redact";

            if (ruleSettings.ContainsKey(RuleKeys.Cases))
            {
                cases = ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString();
            }

            if (ruleSettings.ContainsKey(RuleKeys.OtherValues))
            {
                otherValues = ruleSettings.GetValueOrDefault(RuleKeys.OtherValues)?.ToString();
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
                throw new AnonymizerConfigurationErrorsException("Missing path in Fhir path rule config.");
            }

            if (!ruleSettings.ContainsKey(Constants.MethodKey))
            {
                throw new AnonymizerConfigurationErrorsException("Missing Method in Fhir path rule config.");
            }

            if (!ruleSettings.ContainsKey(RuleKeys.Cases))
            {
                throw new AnonymizerConfigurationErrorsException("Missing Cases in Fhir path rule config.");
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
                throw new AnonymizerConfigurationErrorsException($"Invalid Cases Expression {ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString()}", ex);
            }           

            var supportedOtherValuesOperations = Enum.GetNames(typeof(OtherValuesOperations)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            if (ruleSettings.ContainsKey(RuleKeys.OtherValues) && !supportedOtherValuesOperations.Contains(ruleSettings[RuleKeys.OtherValues]?.ToString()))
            {
                throw new AnonymizerConfigurationErrorsException($"OtherValue value is invalid at {ruleSettings[RuleKeys.OtherValues]}.");
            }
        }
    }
}