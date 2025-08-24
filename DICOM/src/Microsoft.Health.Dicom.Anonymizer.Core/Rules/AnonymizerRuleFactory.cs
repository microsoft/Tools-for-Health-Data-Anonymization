// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerRuleFactory : IAnonymizerRuleFactory
    {
        private readonly AnonymizerDefaultSettings _defaultSettings;

        private readonly Dictionary<string, Dictionary<string, object>> _customSettings;

        private readonly IAnonymizerProcessorFactory _processorFactory;

        public AnonymizerRuleFactory(AnonymizerConfiguration configuration, IAnonymizerProcessorFactory processorFactory)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(processorFactory, nameof(processorFactory));

            _defaultSettings = configuration.DefaultSettings;
            _customSettings = configuration.CustomSettings;
            _processorFactory = processorFactory;
        }

        public AnonymizerRule[] CreateDicomAnonymizationRules(AnonymizerRuleModel[] ruleContents)
        {
            return ruleContents?.Select(entry => CreateDicomAnonymizationRule(entry)).ToArray();
        }

        public AnonymizerRule CreateDicomAnonymizationRule(AnonymizerRuleModel ruleContent)
        {
            EnsureArg.IsNotNull(ruleContent, nameof(ruleContent));

            // Parse and validate method
            if (string.IsNullOrEmpty(ruleContent.Method))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing a required field 'method' in rule config.");
            }

            var method = ruleContent.Method;
            if (!Constants.BuiltInMethods.Contains(method) && !GetCustomMethods().Contains(method))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationRule, $"Anonymization method '{method}' is not supported.");
            }

            // Parse and validate settings.
            Dictionary<string, object> ruleSetting = ExtractRuleSetting(ruleContent, method);

            // Parse and validate tag
            if (string.IsNullOrEmpty(ruleContent.Tag))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing a required field 'tag' in rule config.");
            }

            var createRuleFuncs =
                new Func<string, string, string, IAnonymizerProcessorFactory, Dictionary<string, object>, AnonymizerRule>[]
            {
                TryCreateRule<DicomTag, AnonymizerTagRule>,
                TryCreateRule<DicomMaskedTag, AnonymizerMaskedTagRule>,
                TryCreateRule<DicomVR, AnonymizerVRRule>,
                TryCreateTagNameRule,
            };

            var tagContent = ruleContent.Tag;
            var ruleDescription = JsonConvert.SerializeObject(ruleContent);
            
            foreach (var func in createRuleFuncs)
            {
                var rule = func(tagContent, method, ruleDescription, _processorFactory, ruleSetting);
                if (rule != null)
                {
                    return rule;
                }
            }

            throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, $"Invalid tag '{tagContent}' in rule config.");
        }

        private HashSet<string> GetCustomMethods()
        {
            var processorField = _processorFactory.GetType().GetField("_customProcessors", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (processorField == null)
            {
                return new HashSet<string>();
            }

            var processors = processorField.GetValue(_processorFactory) as Dictionary<string, Type>;
            return processors.Select(x => x.Key).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, object> ExtractRuleSetting(AnonymizerRuleModel ruleContent, string method)
        {
            Dictionary<string, object> parameters = ruleContent.Parameters ?? new Dictionary<string, object>();

            Dictionary<string, object> ruleSetting = _defaultSettings?.GetDefaultSetting(method);
            if (!string.IsNullOrEmpty(ruleContent.Setting))
            {
                if (_customSettings == null || !_customSettings.ContainsKey(ruleContent.Setting))
                {
                    throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingRuleSettings, $"Customized setting {ruleContent.Setting} not defined.");
                }

                ruleSetting = _customSettings[ruleContent.Setting];
            }

            if (ruleSetting == null)
            {
                ruleSetting = parameters;
            }
            else
            {
                // Merge parameters into ruleSetting
                foreach (var param in parameters)
                {
                    ruleSetting[param.Key] = param.Value;
                }
            }

            return ruleSetting;
        }

        private static AnonymizerRule TryCreateRule<TItem, TResult>(
            string tagContent,
            string method,
            string description,
            IAnonymizerProcessorFactory processorFactory,
            Dictionary<string, object> ruleSetting)
        {
            object outputTag;
            try
            {
                outputTag = (TItem)typeof(TItem).GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { tagContent });
            }
            catch
            {
                return null;
            }

            try
            {
                // Convert Dictionary to JObject for compatibility with existing rule constructors
                var jObject = ruleSetting != null ? Newtonsoft.Json.Linq.JObject.FromObject(ruleSetting) : null;
                
                return (AnonymizerRule)Activator.CreateInstance(
                    typeof(TResult),
                    new object[] { outputTag, method, description, processorFactory, jObject });
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        private static AnonymizerRule TryCreateTagNameRule(string tagContent, string method, string description, IAnonymizerProcessorFactory processorFactory, Dictionary<string, object> ruleSetting)
        {
            var nameField = typeof(DicomTag).GetField(tagContent);
            if (nameField != null)
            {
                var tag = (DicomTag)nameField.GetValue(null);
                var jObject = ruleSetting != null ? Newtonsoft.Json.Linq.JObject.FromObject(ruleSetting) : null;
                return new AnonymizerTagRule(tag, method, description, processorFactory, jObject);
            }

            var retiredNameField = typeof(DicomTag).GetField(tagContent + "RETIRED");
            if (retiredNameField != null)
            {
                var tag = (DicomTag)retiredNameField.GetValue(null);
                var jObject = ruleSetting != null ? Newtonsoft.Json.Linq.JObject.FromObject(ruleSetting) : null;
                return new AnonymizerTagRule(tag, method, description, processorFactory, jObject);
            }

            return null;
        }
    }
}