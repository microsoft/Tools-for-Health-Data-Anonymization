using System;
using System.Text.RegularExpressions;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerConfigurationValidator
    {
        public void Validate(AnonymizerConfiguration config)
        {
            if (config.FhirPathRules == null)
            {
                throw new AnonymizerConfigurationErrorsException("The configuration is invalid, please specify any fhirPathRules");
            }

            FhirPathCompiler compiler = new FhirPathCompiler();
            foreach (var rule in config.FhirPathRules)
            {
                if (!rule.ContainsKey(Constants.PathKey) || !rule.ContainsKey(Constants.MethodKey))
                {
                    throw new AnonymizerConfigurationErrorsException("Missing path or method in Fhir path rule config.");
                }

                // Grammar check on FHIR path
                try
                {
                    compiler.Compile(rule[Constants.PathKey]);
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationErrorsException($"Invalid FHIR path {rule[Constants.PathKey]}", ex);
                }

                // Method validate
                string method = rule[Constants.MethodKey];
                if (!Enum.TryParse<AnonymizerMethod>(method, true, out _))
                {
                    throw new AnonymizerConfigurationErrorsException($"{method} not support.");
                }
            }
        }
    }
}
