// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerEngineOptions _anonymizerSettings;
        private readonly AnonymizerRule[] _rules;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
        {
            EnsureArg.IsNotNull(configFilePath, nameof(configFilePath));

            var configuration = LoadConfigurationFromFile(configFilePath);
            
            _anonymizerSettings = anonymizerSettings ?? new AnonymizerEngineOptions();
            ruleFactory ??= new AnonymizerRuleFactory(configuration, processorFactory ?? new DicomProcessorFactory());
            _rules = ruleFactory.CreateDicomAnonymizationRules(configuration.RuleContent);
            _logger.LogDebug("Successfully initialized anonymizer engine.");
        }

        public AnonymizerEngine(IConfiguration configuration, AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            
            // For compatibility, we'll convert the IConfiguration to AnonymizerConfiguration
            // This provides the IConfiguration interface while maintaining existing functionality
            var config = ToAnonymizerConfiguration(configuration);
            
            _anonymizerSettings = anonymizerSettings ?? new AnonymizerEngineOptions();
            ruleFactory ??= new AnonymizerRuleFactory(config, processorFactory ?? new DicomProcessorFactory());
            _rules = ruleFactory.CreateDicomAnonymizationRules(config.RuleContent);
            _logger.LogDebug("Successfully initialized anonymizer engine.");
        }

        private static AnonymizerConfiguration ToAnonymizerConfiguration(IConfiguration configuration)
        {
            // Simple approach: rebuild the JSON structure from IConfiguration and deserialize
            var rules = new List<AnonymizerRuleModel>();
            var rulesSection = configuration.GetSection("rules");
            
            foreach (var ruleSection in rulesSection.GetChildren())
            {
                var rule = new AnonymizerRuleModel
                {
                    Tag = ruleSection["tag"],
                    Method = ruleSection["method"],
                    Setting = ruleSection["setting"]
                };
                
                // Handle parameters
                var paramsSection = ruleSection.GetSection("params");
                if (paramsSection.Exists())
                {
                    rule.Parameters = new Dictionary<string, object>();
                    foreach (var param in paramsSection.GetChildren())
                    {
                        rule.Parameters[param.Key] = param.Value;
                    }
                }
                
                rules.Add(rule);
            }
            
            // Handle default settings
            var defaultSettings = new AnonymizerDefaultSettings();
            var defaultSettingsSection = configuration.GetSection("defaultSettings");
            if (defaultSettingsSection.Exists())
            {
                defaultSettings.PerturbDefaultSetting = ConvertToSettings<PerturbSettings>(defaultSettingsSection.GetSection("perturb"));
                defaultSettings.SubstituteDefaultSetting = ConvertToSettings<SubstituteSettings>(defaultSettingsSection.GetSection("substitute"));
                defaultSettings.DateShiftDefaultSetting = ConvertToSettings<DateShiftSettings>(defaultSettingsSection.GetSection("dateshift"));
                defaultSettings.EncryptDefaultSetting = ConvertToSettings<EncryptSettings>(defaultSettingsSection.GetSection("encrypt"));
                defaultSettings.CryptoHashDefaultSetting = ConvertToSettings<CryptoHashSettings>(defaultSettingsSection.GetSection("cryptoHash"));
                defaultSettings.RedactDefaultSetting = ConvertToSettings<RedactSettings>(defaultSettingsSection.GetSection("redact"));
            }
            
            // Handle custom settings
            var customSettings = new Dictionary<string, Dictionary<string, object>>();
            var customSettingsSection = configuration.GetSection("customSettings");
            if (customSettingsSection.Exists())
            {
                foreach (var setting in customSettingsSection.GetChildren())
                {
                    var settingDict = new Dictionary<string, object>();
                    foreach (var item in setting.GetChildren())
                    {
                        settingDict[item.Key] = item.Value;
                    }
                    customSettings[setting.Key] = settingDict;
                }
            }
            
            return new AnonymizerConfiguration
            {
                RuleContent = rules.ToArray(),
                DefaultSettings = defaultSettings,
                CustomSettings = customSettings
            };
        }
        
        private static T ConvertToSettings<T>(IConfigurationSection section) where T : new()
        {
            if (!section.Exists()) return default(T);
            
            var settings = new T();
            section.Bind(settings);
            return settings;
        }

        public AnonymizerEngine(AnonymizerConfiguration configuration, AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            
            _anonymizerSettings = anonymizerSettings ?? new AnonymizerEngineOptions();
            ruleFactory ??= new AnonymizerRuleFactory(configuration, processorFactory ?? new DicomProcessorFactory());
            _rules = ruleFactory.CreateDicomAnonymizationRules(configuration.RuleContent);
            _logger.LogDebug("Successfully initialized anonymizer engine.");
        }

        private static AnonymizerConfiguration LoadConfigurationFromFile(string configFilePath)
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile(configFilePath, optional: false, reloadOnChange: false);
            
            var config = configBuilder.Build();
            
            // Read the JSON content directly and deserialize with Newtonsoft.Json for compatibility
            var jsonContent = File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<AnonymizerConfiguration>(jsonContent);
        }

        public void AnonymizeDataset(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            // Validate input dataset.
            if (_anonymizerSettings.ValidateInput)
            {
                dataset.Validate();
            }

            var context = InitContext(dataset);
            DicomUtility.DisableAutoValidation(dataset);

            foreach (var rule in _rules)
            {
                rule.Handle(dataset, context);
                _logger.LogDebug($"Successfully handled rule {rule.Description}.");
            }

            // Validate output dataset.
            if (_anonymizerSettings.ValidateOutput)
            {
                dataset.Validate();
            }
        }

        private ProcessContext InitContext(DicomDataset dataset)
        {
            var context = new ProcessContext
            {
                StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            };
            return context;
        }
    }
}
