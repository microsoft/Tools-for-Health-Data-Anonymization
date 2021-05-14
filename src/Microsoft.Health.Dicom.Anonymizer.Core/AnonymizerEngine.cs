// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly Dictionary<string, IAnonymizerProcessor> _processors = new Dictionary<string, IAnonymizerProcessor> { };
        private readonly AnonymizerDicomTagRule[] _rulesByTag;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerSettings _anonymizerSettings;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerSettings anonymizerSettings = null)
            : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath), anonymizerSettings)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerSettings anonymizerSettings = null)
        {
            _anonymizerSettings = anonymizerSettings ?? new AnonymizerSettings();
            InitializeProcessors(configurationManager.GetDefaultSettings());
            _rulesByTag = configurationManager.DicomTagRules;
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public void Anonymize(DicomDataset dataset)
        {
            ValidateInput(dataset);

            // Extract SOPInstanceUID, SereisInstanceUID and StudyInstanceUID
            var basicInfo = ExtractBasicInformation(dataset);
            dataset.AutoValidate = _anonymizerSettings.AutoValidate;

            var curDataset = dataset.ToArray();
            foreach (var item in curDataset)
            {
                var ruleByTag = _rulesByTag?.Where(r => string.Equals(item.Tag.DictionaryEntry.Keyword, r.Tag?.DictionaryEntry.Keyword, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(item.ValueRepresentation.Code, r.VR?.Code, StringComparison.InvariantCultureIgnoreCase)
                || (r.IsMasked && r.MaskedTag.IsMatch(item.Tag))).FirstOrDefault();
                if (ruleByTag != null)
                {
                    string method = ruleByTag.Method.ToUpperInvariant();
                    if (!_processors.ContainsKey(method))
                    {
                        continue;
                    }

                    try
                    {
                        _processors[method].Process(dataset, item, basicInfo, ruleByTag.RuleSetting);
                        _logger.LogDebug("{0,-15}{1,-40}{2,-15}{3,-50}{4,-75}", "(" + string.Format("{0,4:X4}", item.Tag.Group) + "," + string.Format("{0,4:X4}", item.Tag.Element) + ")", item.Tag.DictionaryEntry.Name, method, item is DicomElement ? ((DicomElement)item).Get<string>() : "sequence", dataset.GetSingleValueOrDefault<string>(item.Tag, string.Empty));
                    }
                    catch (Exception ex)
                    {
                        if (_anonymizerSettings.SkipFailedItem)
                        {
                            _logger.LogWarning($"Fail to anonymize Item {item.Tag.DictionaryEntry.Name} using {method} method. The original value will be kept.", ex);
                        }
                        else
                        {
                            throw;
                        }
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

        private DicomBasicInformation ExtractBasicInformation(DicomDataset dataset)
        {
            var basicInfo = new DicomBasicInformation
            {
                StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            };
            return basicInfo;
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
