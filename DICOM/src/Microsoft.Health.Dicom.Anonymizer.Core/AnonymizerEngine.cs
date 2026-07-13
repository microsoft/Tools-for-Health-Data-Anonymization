// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerEngineOptions _anonymizerSettings;
        private readonly bool _requireRuntimeKeys;
        private readonly AnonymizerRule[] _rules;
        private readonly bool _usesCryptoHash;
        private readonly bool _usesDateShift;
        private readonly bool _usesEncrypt;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
            : this(configFilePath, anonymizerSettings, ruleFactory, processorFactory, false)
        {
        }

        public AnonymizerEngine(string configFilePath, bool requireRuntimeKeys)
            : this(configFilePath, null, null, null, requireRuntimeKeys)
        {
        }

        public AnonymizerEngine(string configFilePath, AnonymizerEngineOptions anonymizerSettings, IAnonymizerRuleFactory ruleFactory, IAnonymizerProcessorFactory processorFactory, bool requireRuntimeKeys)
            : this(AnonymizerConfigurationManager.CreateFromJsonFile(configFilePath), anonymizerSettings, ruleFactory, processorFactory, requireRuntimeKeys)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null, IAnonymizerProcessorFactory processorFactory = null)
            : this(configurationManager, anonymizerSettings, ruleFactory, processorFactory, false)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, bool requireRuntimeKeys)
            : this(configurationManager, null, null, null, requireRuntimeKeys)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerEngineOptions anonymizerSettings, IAnonymizerRuleFactory ruleFactory, IAnonymizerProcessorFactory processorFactory, bool requireRuntimeKeys)
        {
            EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));

            _anonymizerSettings = anonymizerSettings ?? new AnonymizerEngineOptions();
            _requireRuntimeKeys = requireRuntimeKeys;
            (_usesCryptoHash, _usesDateShift, _usesEncrypt) = GetConfiguredKeyedMethods(configurationManager.Configuration.RuleContent);

            ruleFactory ??= new AnonymizerRuleFactory(configurationManager.Configuration, processorFactory ?? new DicomProcessorFactory());
            _rules = ruleFactory.CreateDicomAnonymizationRules(configurationManager.Configuration.RuleContent);
            _logger.LogDebug("Successfully initialized anonymizer engine.");
        }

        public void AnonymizeDataset(DicomDataset dataset)
        {
            AnonymizeDataset(dataset, null);
        }

        public void AnonymizeDataset(DicomDataset dataset, RuntimeKeySettings runtimeKeySettings)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            ValidateRequiredRuntimeKeys(runtimeKeySettings);

            // Validate input dataset.
            if (_anonymizerSettings.ValidateInput)
            {
                dataset.Validate();
            }

            var context = InitContext(dataset);
            context.RuntimeKeys = runtimeKeySettings;
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

        private static (bool UsesCryptoHash, bool UsesDateShift, bool UsesEncrypt) GetConfiguredKeyedMethods(JObject[] ruleContents)
        {
            var usesCryptoHash = false;
            var usesDateShift = false;
            var usesEncrypt = false;

            if (ruleContents == null)
            {
                return (usesCryptoHash, usesDateShift, usesEncrypt);
            }

            foreach (var ruleContent in ruleContents)
            {
                if (ruleContent == null || !ruleContent.TryGetValue(Constants.MethodKey, StringComparison.OrdinalIgnoreCase, out JToken methodToken))
                {
                    continue;
                }

                if (string.Equals(methodToken?.ToString(), nameof(AnonymizerMethod.CryptoHash), StringComparison.OrdinalIgnoreCase))
                {
                    usesCryptoHash = true;
                }
                else if (string.Equals(methodToken?.ToString(), nameof(AnonymizerMethod.DateShift), StringComparison.OrdinalIgnoreCase))
                {
                    usesDateShift = true;
                }
                else if (string.Equals(methodToken?.ToString(), nameof(AnonymizerMethod.Encrypt), StringComparison.OrdinalIgnoreCase))
                {
                    usesEncrypt = true;
                }

                if (usesCryptoHash && usesDateShift && usesEncrypt)
                {
                    break;
                }
            }

            return (usesCryptoHash, usesDateShift, usesEncrypt);
        }

        private void ValidateRequiredRuntimeKeys(RuntimeKeySettings runtimeKeySettings)
        {
            if (!_requireRuntimeKeys)
            {
                return;
            }

            if (_usesCryptoHash && string.IsNullOrWhiteSpace(runtimeKeySettings?.CryptoHashKey))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Runtime cryptoHashKey is required when requireRuntimeKeys is enabled and a cryptoHash method is configured.");
            }

            if (_usesDateShift && string.IsNullOrWhiteSpace(runtimeKeySettings?.DateShiftKey))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Runtime dateShiftKey is required when requireRuntimeKeys is enabled and a dateShift method is configured.");
            }

            if (_usesEncrypt && string.IsNullOrWhiteSpace(runtimeKeySettings?.EncryptKey))
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Runtime encryptKey is required when requireRuntimeKeys is enabled and an encrypt method is configured.");
            }
        }
    }
}
