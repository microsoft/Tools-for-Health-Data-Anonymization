// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly Dictionary<string, IAnonymizerProcessor> _processors = new Dictionary<string, IAnonymizerProcessor> { };
        private readonly AnonymizerDicomTagRule[] _rulesByTag;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerSettings _anonymizerSettings;
        private readonly AnonymizerRuleHandler _ruleHandler;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerSettings anonymizerSettings = null)
            : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath), anonymizerSettings)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerSettings anonymizerSettings = null)
        {
            _anonymizerSettings = anonymizerSettings ?? new AnonymizerSettings();
            InitializeProcessors(configurationManager.GetDefaultSettings());
            _rulesByTag = configurationManager.DicomTagRules;
            _ruleHandler = new AnonymizerRuleHandler(_rulesByTag, _processors)
            {
                SkipFailedItem = _anonymizerSettings.SkipFailedItem,
            };
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public void AnonymizeDateset(DicomDataset dataset)
        {
            ValidateInput(dataset);
            dataset.AutoValidate = _anonymizerSettings.AutoValidate;
            _ruleHandler.Handle(dataset);
        }

        private void ValidateInput(DicomDataset dataset)
        {
            if (_anonymizerSettings.ValidateInput)
            {
                dataset.Validate();
            }
        }


        private void InitializeProcessors(AnonymizerDefaultSettings defaultSettings)
        {
            _processors.Add(AnonymizerMethod.Redact.ToString().ToUpperInvariant(), new RedactProcessor(defaultSettings.RedactDefaultSetting));
            _processors.Add(AnonymizerMethod.Keep.ToString().ToUpperInvariant(), new KeepProcessor());
            _processors.Add(AnonymizerMethod.Remove.ToString().ToUpperInvariant(), new RemoveProcessor());
            _processors.Add(AnonymizerMethod.RefreshUID.ToString().ToUpperInvariant(), new RefreshUIDProcessor());
            _processors.Add(AnonymizerMethod.Substitute.ToString().ToUpperInvariant(), new SubstituteProcessor(defaultSettings.SubstituteDefaultSetting));
            _processors.Add(AnonymizerMethod.Perturb.ToString().ToUpperInvariant(), new PerturbProcessor(defaultSettings.PerturbDefaultSetting));
            _processors.Add(AnonymizerMethod.Encrypt.ToString().ToUpperInvariant(), new EncryptionProcessor(defaultSettings.EncryptDefaultSetting));
            _processors.Add(AnonymizerMethod.CryptoHash.ToString().ToUpperInvariant(), new CryptoHashProcessor(defaultSettings.CryptoHashDefaultSetting));
            _processors.Add(AnonymizerMethod.DateShift.ToString().ToUpperInvariant(), new DateShiftProcessor(defaultSettings.DateShiftDefaultSetting));
        }
    }
}
