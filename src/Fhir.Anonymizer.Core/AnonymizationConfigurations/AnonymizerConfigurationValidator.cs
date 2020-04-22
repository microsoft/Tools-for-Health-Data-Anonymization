using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Hl7.Fhir.Model;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerConfigurationValidator
    {
        private readonly Regex s_fhirPathRegex = new Regex(@"^[a-zA-Z]+(\.([a-zA-Z]+))*$");

        public void Validate(AnonymizerConfiguration config)
        {
            if (config.TypeRules == null && config.PathRules == null)
            {
                throw new AnonymizerConfigurationErrorsException("The configuration is invalid, please specify any pathRules or typeRules");
            }

            var invalidPaths = GetInvalidPaths(config);
            var invalidTypes = GetInvalidTypes(config);
            var invalidMethods = GetInvalidMethods(config);
            var invalidDateShiftTypes = GetInvalidDateShiftTypes(config);

            StringBuilder builder = new StringBuilder(string.Empty);
            if (invalidPaths.Count > 0)
            {
                builder.Append($"The specified path is unsupported: {String.Join(", ", invalidPaths)}. ");
            }
            if (invalidTypes.Count > 0)
            {
                builder.Append($"The specified type is unsupported: {String.Join(", ", invalidTypes)}. ");
            }
            if (invalidMethods.Count > 0)
            {
                builder.Append($"The specified method is unsupported: {String.Join(", ", invalidMethods)}. ");
            }
            if (invalidDateShiftTypes.Count > 0)
            {
                builder.Append($"Date shift will not take effect on types: {String.Join(", ", invalidDateShiftTypes)}");
            }

            var exceptionMessage = builder.ToString();
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                throw new AnonymizerConfigurationErrorsException(exceptionMessage);
            }
        }

        private List<string> GetInvalidPaths(AnonymizerConfiguration config)
        {
            var invalidPaths = new List<string>();
            if (config.PathRules != null)
            {
                foreach (var path in config.PathRules.Keys)
                {
                    if (!s_fhirPathRegex.IsMatch(path))
                    {
                        invalidPaths.Add(path);
                    }
                }
            }

            return invalidPaths;
        }

        private List<string> GetInvalidTypes(AnonymizerConfiguration config)
        {
            var invalidTypes = new List<string>();
            if (config.TypeRules != null)
            {
                foreach (var type in config.TypeRules.Keys)
                {
                    if (!Enum.TryParse<FHIRAllTypes>(type, true, out _))
                    {
                        invalidTypes.Add(type);
                    }
                }
            }

            return invalidTypes;
        }

        private List<string> GetInvalidMethods(AnonymizerConfiguration config)
        {
            var invalidMethods = new List<string>();

            if (config.PathRules != null)
            {
                foreach (var method in config.PathRules.Values)
                {
                    if (!Enum.TryParse<AnonymizerMethod>(method, true, out _))
                    {
                        invalidMethods.Add(method);
                    }
                }
            }

            if (config.TypeRules != null)
            {
                foreach (var method in config.TypeRules.Values)
                {
                    if (!Enum.TryParse<AnonymizerMethod>(method, true, out _))
                    {
                        invalidMethods.Add(method);
                    }
                }
            }

            return invalidMethods;
        }

        private List<string> GetInvalidDateShiftTypes(AnonymizerConfiguration config)
        {
            var invalidTypes = new List<string>();
            if (config.TypeRules != null)
            {
                foreach (var rule in config.TypeRules)
                {
                    if (Enum.TryParse<AnonymizerMethod>(rule.Value, true, out AnonymizerMethod method))
                    {
                        if (method == AnonymizerMethod.DateShift && !IsValidDateShiftType(rule.Key))
                        {
                            invalidTypes.Add(rule.Key);
                        }
                    }
                }
            }

            return invalidTypes;
        }

        private bool IsValidDateShiftType(string type)
        {
            return string.Equals(type, FHIRAllTypes.Date.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(type, FHIRAllTypes.DateTime.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(type, FHIRAllTypes.Instant.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
