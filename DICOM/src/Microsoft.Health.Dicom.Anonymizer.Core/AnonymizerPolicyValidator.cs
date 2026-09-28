// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    internal static class AnonymizerPolicyValidator
    {
        private static readonly HashSet<string> RuleFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Constants.TagKey,
            Constants.MethodKey,
            Constants.Parameters,
            Constants.RuleSetting,
        };

        internal static void ValidateRuleFields(JObject[] ruleContents)
        {
            if (ruleContents == null)
            {
                return;
            }

            for (var index = 0; index < ruleContents.Length; index++)
            {
                var rule = ruleContents[index];
                var observedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (rule != null && rule.Properties().Any(property => !observedFields.Add(property.Name)))
                {
                    throw new AnonymizerConfigurationException(
                        DicomAnonymizationErrorCode.InvalidConfigurationValues,
                        $"Policy validation failed for rule {index}: duplicate field.");
                }

                if (rule == null || rule.Properties().Any(property => !RuleFields.Contains(property.Name)))
                {
                    throw new AnonymizerConfigurationException(
                        DicomAnonymizationErrorCode.InvalidConfigurationValues,
                        $"Policy validation failed for rule {index}: unknown field.");
                }
            }
        }

        internal static void ValidateRules(AnonymizerRule[] rules)
        {
            if (rules == null)
            {
                return;
            }

            var selectors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < rules.Length; index++)
            {
                var rule = rules[index];
                var selector = GetSelector(rule);
                if (selectors.TryGetValue(selector, out var previousIndex))
                {
                    throw new AnonymizerConfigurationException(
                        DicomAnonymizationErrorCode.InvalidConfigurationValues,
                        $"Policy validation failed: duplicate selector at rules {previousIndex} and {index}.");
                }

                selectors.Add(selector, index);
                ValidateBroadRule(rule, index);
                ValidateCryptoHashRule(rule, index);
            }
        }

        private static string GetSelector(AnonymizerRule rule)
        {
            return rule switch
            {
                AnonymizerTagRule tagRule => $"tag:{tagRule.Tag.Group:X4}{tagRule.Tag.Element:X4}",
                AnonymizerMaskedTagRule maskedRule => $"mask:{maskedRule.MaskedTag}",
                AnonymizerVRRule vrRule => $"vr:{vrRule.VR.Code}",
                _ => $"rule:{rule.GetType().Name}:{rule.Description}",
            };
        }

        private static void ValidateBroadRule(AnonymizerRule rule, int index)
        {
            if (rule is not AnonymizerVRRule vrRule)
            {
                return;
            }

            if (vrRule.VR == DicomVR.UI && !string.Equals(rule.Method, "keep", StringComparison.OrdinalIgnoreCase))
            {
                throw new AnonymizerConfigurationException(
                    DicomAnonymizationErrorCode.InvalidConfigurationValues,
                    $"Policy validation failed for rule {index}: broad UI transformation is unsafe.");
            }

            if (vrRule.VR == DicomVR.SQ && string.Equals(rule.Method, "remove", StringComparison.OrdinalIgnoreCase))
            {
                throw new AnonymizerConfigurationException(
                    DicomAnonymizationErrorCode.InvalidConfigurationValues,
                    $"Policy validation failed for rule {index}: broad SQ removal is unsafe.");
            }
        }

        private static void ValidateCryptoHashRule(AnonymizerRule rule, int index)
        {
            if (!string.Equals(rule.Method, "cryptoHash", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (rule is AnonymizerMaskedTagRule)
            {
                throw new AnonymizerConfigurationException(
                    DicomAnonymizationErrorCode.InvalidConfigurationValues,
                    $"Policy validation failed for rule {index}: cryptoHash compatibility cannot be proven for a masked selector.");
            }

            IEnumerable<DicomVR> valueRepresentations = rule switch
            {
                AnonymizerVRRule vrRule => new[] { vrRule.VR },
                AnonymizerTagRule tagRule => tagRule.Tag.DictionaryEntry?.ValueRepresentations ?? Array.Empty<DicomVR>(),
                _ => Array.Empty<DicomVR>(),
            };

            if (valueRepresentations.Any(vr => vr == DicomVR.DA || vr == DicomVR.DT || vr == DicomVR.TM || vr == DicomVR.AS))
            {
                throw new AnonymizerConfigurationException(
                    DicomAnonymizationErrorCode.InvalidConfigurationValues,
                    $"Policy validation failed for rule {index}: cryptoHash does not support fixed-format VR.");
            }
        }
    }
}
