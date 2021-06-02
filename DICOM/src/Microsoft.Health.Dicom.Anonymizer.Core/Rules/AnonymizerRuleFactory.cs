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
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerRuleFactory : IAnonymizerRuleFactory
    {
        private AnonymizerDefaultSettings _defaultSettings;

        private Dictionary<string, JObject> _customizedSettings;

        private IAnonymizerProcessorFactory _processorFactory;

        public AnonymizerRuleFactory(AnonymizerConfiguration configuration, IAnonymizerProcessorFactory processorFactory)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _defaultSettings = configuration.DefaultSettings;
            _customizedSettings = configuration.CustomizedSettings;
            _processorFactory = processorFactory;
        }

        public AnonymizerRule[] CreateAnonymizationDicomRule(JObject[] rule)
        {
            return rule?.Select(entry => CreateAnonymizationDicomRule(entry)).ToArray();
        }

        public AnonymizerRule CreateAnonymizationDicomRule(JObject rule)
        {
            EnsureArg.IsNotNull(rule, nameof(rule));

            // Parse and validate method
            if (!rule.ContainsKey(Constants.MethodKey))
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing method in rule config");
            }

            var method = rule[Constants.MethodKey].ToString();
            var supportedMethods = Enum.GetNames(typeof(AnonymizerMethod)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            if (!supportedMethods.Contains(method))
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationRule, $"Anonymization method {method} not supported.");
            }

            // Parse and validate settings
            JObject parameters = null;
            if (rule.ContainsKey(Constants.Parameters))
            {
                parameters = rule[Constants.Parameters].ToObject<JObject>();
            }

            IDicomAnonymizationSetting ruleSetting = _defaultSettings.GetDefaultSetting(method);
            if (rule.ContainsKey(Constants.RuleSetting))
            {
                if (_customizedSettings == null || !_customizedSettings.ContainsKey(rule[Constants.RuleSetting].ToString()))
                {
                    throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, $"Customized setting {rule[Constants.RuleSetting]} not defined");
                }

                var settings = _customizedSettings[rule[Constants.RuleSetting].ToString()];

                if (parameters != null)
                {
                    settings.Merge(parameters, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                }

                ruleSetting = AnonymizerDefaultSettings.DicomSettingsMapping[method].CreateFromRuleSettings(settings);
            }
            else if (parameters != null)
            {
                var propertyKeys = parameters.Properties().Select(x => x.Name);
                ruleSetting = _defaultSettings.GetDefaultSetting(method);
                foreach (var prop in ruleSetting.GetType().GetProperties())
                {
                    if (!propertyKeys.Contains(prop.Name, StringComparer.InvariantCultureIgnoreCase))
                    {
                        parameters.Add(prop.Name, prop.GetValue(ruleSetting)?.ToString());
                    }
                }

                ruleSetting = AnonymizerDefaultSettings.DicomSettingsMapping[method].CreateFromRuleSettings(parameters);
            }

            ruleSetting?.Validate();

            // Parse and validate tag
            if (rule.ContainsKey(Constants.TagKey))
            {
                var content = rule[Constants.TagKey].ToString();

                try
                {
                    var tag = DicomTag.Parse(content);
                    return new AnonymizerTagRule(tag, method, ruleSetting, rule.ToString(), _processorFactory);
                }
                catch (Exception)
                {
                    try
                    {
                        var tag = DicomMaskedTag.Parse(content);
                        return new AnonymizerMaskedTagRule(tag, method, ruleSetting, rule.ToString(), _processorFactory);
                    }
                    catch
                    {
                        try
                        {
                            var vr = DicomVR.Parse(content);
                            return new AnonymizerVRRule(vr, method, ruleSetting, rule.ToString(), _processorFactory);
                        }
                        catch
                        {
                            try
                            {
                                var dicomTags = new DicomTag(0, 0);
                                DicomTag tag = (DicomTag)dicomTags.GetType().GetField(content).GetValue(dicomTags);
                                return new AnonymizerTagRule(tag, method, ruleSetting, rule.ToString(), _processorFactory);
                            }
                            catch
                            {
                                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Invaid tag in rule config");
                            }
                        }
                    }
                }
            }
            else
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing tag in rule config");
            }
        }
    }
}
