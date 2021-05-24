// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerRuleHandlers
    {
        public AnonymizerRuleHandlers(DicomTag tag, string method, IDicomAnonymizationSetting ruleSetting, string content)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsNotNull(method, nameof(method));

            Tag = tag;
            Method = method;
            RuleSetting = ruleSetting;
            Content = content;
        }

        public AnonymizerRuleHandlers(DicomMaskedTag tag, string method, IDicomAnonymizationSetting ruleSetting, string content)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsNotNull(method, nameof(method));

            MaskedTag = tag;
            Method = method;
            RuleSetting = ruleSetting;
            IsMasked = true;
            Content = content;
        }

        public AnonymizerRuleHandlers(DicomVR vr, string method, IDicomAnonymizationSetting ruleSetting, string content)
        {
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(method, nameof(method));

            VR = vr;
            Method = method;
            RuleSetting = ruleSetting;
            IsVRRule = true;
            Content = content;
        }

        public string Content { get; set; }

        public DicomVR VR { get; set; }

        public DicomTag Tag { get; set; }

        public DicomMaskedTag MaskedTag { get; set; }

        public string Method { get; set; }

        public bool IsVRRule { get; set; } = false;

        public bool IsMasked { get; set; } = false;

        public IDicomAnonymizationSetting RuleSetting { get; set; }

        public void HandleRule(DicomDataset dataset, ProcessContext context)
        {
            var locatedItems = LocateDicomTag(dataset, context);
            IAnonymizerProcessor processor = Method.ToLower() switch
            {
                "perturb" => new PerturbProcessor(RuleSetting),
                "substitute" => new SubstituteProcessor(RuleSetting),
                "dateshift" => new DateShiftProcessor(RuleSetting),
                "encrypt" => new EncryptionProcessor(RuleSetting),
                "cryptohash" => new CryptoHashProcessor(RuleSetting),
                "redact" => new RedactProcessor(RuleSetting),
                "remove" => new RemoveProcessor(),
                "refreshuid" => new RefreshUIDProcessor(),
                "keep" => new KeepProcessor(),
                _ => null,
            };

            foreach (var item in locatedItems)
            {
                processor.Process(dataset, item, context);
                context.VisitedNodes.Add(item.Tag.ToString());
            }
        }

        private List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
        {
            var locatedItems = new List<DicomItem>() { };
            if (Tag != null)
            {
                locatedItems.Add(dataset.GetDicomItem<DicomItem>(Tag));
            }
            else if (IsVRRule)
            {
                foreach (var item in dataset)
                {
                    if (string.Equals(item.ValueRepresentation.Code, VR?.Code))
                    {
                        locatedItems.Add(item);
                    }
                }
            }
            else if (IsMasked)
            {
                foreach (var item in dataset)
                {
                    if (MaskedTag.IsMatch(item.Tag))
                    {
                        locatedItems.Add(item);
                    }
                }
            }

            return locatedItems.Where(x => x != null && !context.VisitedNodes.Contains(x.Tag.ToString())).ToList();
        }

        public static AnonymizerRuleHandlers CreateAnonymizationDicomRule(JObject rule, AnonymizerConfiguration configuration)
        {
            EnsureArg.IsNotNull(rule, nameof(rule));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

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

            IDicomAnonymizationSetting ruleSetting = configuration.DefaultSettings.GetDefaultSetting(method);
            if (rule.ContainsKey(Constants.RuleSetting))
            {
                if (configuration.CustomizedSettings == null || !configuration.CustomizedSettings.ContainsKey(rule[Constants.RuleSetting].ToString()))
                {
                    throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, $"Customized setting {rule[Constants.RuleSetting]} not defined");
                }

                var settings = configuration.CustomizedSettings[rule[Constants.RuleSetting].ToString()];

                if (parameters != null)
                {
                    settings.Merge(parameters, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                }

                ruleSetting = AnonymizerDefaultSettings.DicomSettingsMapping[method].CreateFromRuleSettings(settings);
            }
            else if (parameters != null)
            {
                var propertyKeys = parameters.Properties().Select(x => x.Name);
                ruleSetting = configuration.DefaultSettings.GetDefaultSetting(method);
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
                    return new AnonymizerRuleHandlers(tag, method, ruleSetting, rule.ToString());
                }
                catch (Exception)
                {
                    try
                    {
                        var tag = DicomMaskedTag.Parse(content);
                        return new AnonymizerRuleHandlers(tag, method, ruleSetting, rule.ToString());
                    }
                    catch
                    {
                        try
                        {
                            var vr = DicomVR.Parse(content);
                            return new AnonymizerRuleHandlers(vr, method, ruleSetting, rule.ToString());
                        }
                        catch
                        {
                            try
                            {
                                var dicomTags = new DicomTag(0, 0);
                                DicomTag tag = (DicomTag)dicomTags.GetType().GetField(content).GetValue(dicomTags);
                                return new AnonymizerRuleHandlers(tag, method, ruleSetting, rule.ToString());
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
