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
                var tagContent = ruleContent[Constants.TagKey].ToString();
                if (TryParseDICOMTag(tagContent, out DicomTag tag))
                {
                    return new AnonymizerTagRule(tag, method, ruleContent.ToString(), _processorFactory, ruleSetting);
                }
                else if (TryParseDICOMMaskedTag(tagContent, out DicomMaskedTag maskedTag))
                {
                    return new AnonymizerMaskedTagRule(maskedTag, method, ruleContent.ToString(), _processorFactory, ruleSetting);
                }
                else if (TryParseDICOMVR(tagContent, out DicomVR vr))
                {
                    return new AnonymizerVRRule(vr, method, ruleContent.ToString(), _processorFactory, ruleSetting);
                }
                else if (TryParseDICOMTagName(tagContent, out DicomTag tagByName))
                {
                    return new AnonymizerTagRule(tagByName, method, ruleContent.ToString(), _processorFactory, ruleSetting);
                }
                else
                {
                    throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Invalid tag in rule config.");
                }
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

        private bool TryParseDICOMTag(string tagContent, out DicomTag output)
        {
            try
            {
                output = DicomTag.Parse(tagContent);
                return true;
            }
            catch
            {
                output = null;
                return false;
            }
        }

        private bool TryParseDICOMMaskedTag(string tagContent, out DicomMaskedTag output)
        {
            try
            {
                output = DicomMaskedTag.Parse(tagContent);
                return true;
            }
            catch
            {
                output = null;
                return false;
            }
        }

        private bool TryParseDICOMVR(string tagContent, out DicomVR output)
        {
            try
            {
                output = DicomVR.Parse(tagContent);
                return true;
            }
            catch
            {
                output = null;
                return false;
            }
        }

        private bool TryParseDICOMTagName(string tagContent, out DicomTag output)
        {
            try
            {
                output = (DicomTag)typeof(DicomTag).GetField(tagContent).GetValue(new DicomTag(0, 0));
                return true;
            }
            catch
            {
                output = null;
                return false;
            }
        }
    }
}
