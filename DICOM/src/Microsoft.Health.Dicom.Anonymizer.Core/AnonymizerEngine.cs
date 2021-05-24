﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly AnonymizerRuleHandlers[] _rules;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerSettings _anonymizerSettings;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerSettings anonymizerSettings = null)
            : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath), anonymizerSettings)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerSettings anonymizerSettings = null)
        {
            _anonymizerSettings = anonymizerSettings ?? new AnonymizerSettings();
            _rules = configurationManager.DicomTagRules;
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public void AnonymizeDateset(DicomDataset dataset)
        {
            var context = ExtractBasicInformation(dataset);
            ValidateInput(dataset);
            dataset.AutoValidate = _anonymizerSettings.AutoValidate;
            foreach (var rule in _rules)
            {
                try
                {
                    rule.HandleRule(dataset, context);
                }
                catch (Exception ex)
                {
                    if (_anonymizerSettings.SkipFailedItem)
                    {
                        _logger.LogWarning($"Fail to handle rule {rule.Content}.", ex.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private void ValidateInput(DicomDataset dataset)
        {
            if (_anonymizerSettings.ValidateInput)
            {
                dataset.Validate();
            }
        }

        private ProcessContext ExtractBasicInformation(DicomDataset dataset)
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
