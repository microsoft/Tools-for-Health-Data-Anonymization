using System;
using System.Collections.Generic;
using EnsureThat;

namespace Fhir.Anonymizer.Core.Processors.Settings
{
    public class PerturbSetting
    {
        public double Span { get; set; }
        public PerturbRangeType RangeType { get; set; }
        public int RoundTo { get; set; }

        public static PerturbSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var roundTo = 2;
            if (ruleSettings.ContainsKey(RuleKeys.RoundTo)) 
            {
                roundTo = Convert.ToInt32(ruleSettings.GetValueOrDefault(RuleKeys.RoundTo)?.ToString());
            }

            double span = 0;
            if (ruleSettings.ContainsKey(RuleKeys.Span))
            {
                span = Convert.ToDouble(ruleSettings.GetValueOrDefault(RuleKeys.Span)?.ToString());
            }

            var rangeType = PerturbRangeType.Fixed;
            if (string.Equals(PerturbRangeType.Proportional.ToString(), 
                ruleSettings.GetValueOrDefault(RuleKeys.RangeType)?.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
            {
                rangeType = PerturbRangeType.Proportional;
            }

            return new PerturbSetting
            {
                Span = span,
                RangeType = rangeType,
                RoundTo = roundTo
            };
        }
    }
}
