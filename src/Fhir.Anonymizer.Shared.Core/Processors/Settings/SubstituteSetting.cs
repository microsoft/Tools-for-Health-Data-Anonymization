using System.Collections.Generic;
using EnsureThat;

namespace Fhir.Anonymizer.Core.Processors.Settings
{
    public class SubstituteSetting
    {
        public string ReplaceWith { get; set; }

        public static SubstituteSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            string replaceWith = ruleSettings.GetValueOrDefault(Constants.ReplaceWithKey)?.ToString();
            return new SubstituteSetting
            {
                ReplaceWith = replaceWith
            };
        }
    }
}
