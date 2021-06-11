// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerRuleFactory : IAnonymizerRuleFactory
    {
        private readonly AnonymizerDefaultSettings _defaultSettings;

        private readonly Dictionary<string, JObject> _customSettings;

        private readonly IAnonymizerProcessorFactory _processorFactory;

        private static readonly HashSet<string> _supportedMethods = Enum.GetNames(typeof(AnonymizerMethod)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        public AnonymizerRuleFactory(AnonymizerConfiguration configuration, IAnonymizerProcessorFactory processorFactory)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(processorFactory, nameof(processorFactory));

            _defaultSettings = configuration.DefaultSettings;
            _customSettings = configuration.CustomSettings;
            _processorFactory = processorFactory;
        }

        public AnonymizerRule[] CreateAnonymizationDicomRules(JObject[] ruleContents)
        {
            return ruleContents?.Select(entry => CreateAnonymizationDicomRule(entry)).ToArray();
        }

        public AnonymizerRule CreateAnonymizationDicomRule(JObject ruleContent)
        {
            EnsureArg.IsNotNull(ruleContent, nameof(ruleContent));

            // Parse and validate method
            if (!ruleContent.ContainsKey(Constants.MethodKey))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing method in rule config.");
            }

            var method = ruleContent[Constants.MethodKey].ToString();
            if (!_supportedMethods.Contains(method))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationRule, $"Anonymization method {method} not supported.");
            }

            // Parse and validate settings
            JObject ruleSetting = ExtractRuleSetting(ruleContent, method);

            // Parse and validate tag
            if (ruleContent.ContainsKey(Constants.TagKey))
            {
                var createRuleFuncs =
                    new Func<string, string, string, IAnonymizerProcessorFactory, JObject, AnonymizerRule>[]
                {
                    TryCreateRule<DicomTag, AnonymizerTagRule>,
                    TryCreateRule<DicomMaskedTag, AnonymizerMaskedTagRule>,
                    TryCreateRule<DicomVR, AnonymizerVRRule>,
                    TryCreateTagNameRule,
                };

                var tagContent = ruleContent[Constants.TagKey].ToString();
                foreach (var func in createRuleFuncs)
                {
                    var rule = func(tagContent, method, ruleContent.ToString(), _processorFactory, ruleSetting);
                    if (rule != null)
                    {
                        return rule;
                    }
                }

                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Invalid tag in rule config.");
            }
            else
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing tag in rule config.");
            }
        }

        private JObject ExtractRuleSetting(JObject ruleContent, string method)
        {
            JObject parameters = null;
            if (ruleContent.ContainsKey(Constants.Parameters))
            {
                parameters = ruleContent[Constants.Parameters].ToObject<JObject>();
            }

            JObject ruleSetting = _defaultSettings.GetDefaultSetting(method);
            if (ruleContent.ContainsKey(Constants.RuleSetting))
            {
                if (_customSettings == null || !_customSettings.ContainsKey(ruleContent[Constants.RuleSetting].ToString()))
                {
                    throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingRuleSettings, $"Customized setting {ruleContent[Constants.RuleSetting]} not defined.");
                }

                ruleSetting = _customSettings[ruleContent[Constants.RuleSetting].ToString()];
            }

            if (ruleSetting == null)
            {
                ruleSetting = parameters;
            }
            else
            {
                ruleSetting.Merge(parameters, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
            }

            return ruleSetting;
        }

        private static AnonymizerRule TryCreateRule<TItem, TResult>(
            string tagContent,
            string method,
            string description,
            IAnonymizerProcessorFactory processorFactory,
            JObject ruleSetting)
        {
            try
            {
                var output = (TItem)typeof(TItem).GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { tagContent });
                return (AnonymizerRule)Activator.CreateInstance(
                    typeof(TResult),
                    new object[] { output, method, description, processorFactory, ruleSetting });
            }
            catch
            {
                return null;
            }
        }

        private static AnonymizerRule TryCreateTagNameRule(string tagContent, string method, string description, IAnonymizerProcessorFactory processorFactory, JObject ruleSetting)
        {
            try
            {
                var output = (DicomTag)typeof(DicomTag).GetField(tagContent).GetValue(new DicomTag(0, 0));
                return new AnonymizerTagRule(output, method, description, processorFactory, ruleSetting);
            }
            catch
            {
                return null;
            }
        }
    }
}
