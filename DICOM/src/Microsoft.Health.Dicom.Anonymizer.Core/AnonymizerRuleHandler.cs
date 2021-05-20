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
    public class AnonymizerRuleHandler
    {
        private AnonymizerDicomTagRule[] _rulesByTag;
        private Dictionary<string, IAnonymizerProcessor> _processors;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerRuleHandler>();

        public AnonymizerRuleHandler(AnonymizerDicomTagRule[] rulesByTag, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _rulesByTag = rulesByTag;
            _processors = processors;
        }

        public bool SkipFailedItem { get; set; } = true;

        public void Handle(DicomDataset dataset)
        {
            // Extract SOPInstanceUID, SereisInstanceUID and StudyInstanceUID
            var basicInfo = ExtractBasicInformation(dataset);

            var curDataset = dataset.ToArray();
            foreach (var item in curDataset)
            {
                var ruleByTag = SelectDicomRule(item);
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
                        _logger.LogDebug($"Dicom tag {item.Tag.DictionaryEntry.Name} perform {method} operation");
                        if (string.Equals(method, "redact", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "remove", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug($"Value is anonymized from {(item is DicomElement element ? element.Get<string>() : "sequence")} to {dataset.GetSingleValueOrDefault<string>(item.Tag, string.Empty)}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (SkipFailedItem)
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

        private AnonymizerDicomTagRule SelectDicomRule(DicomItem item)
        {
            foreach ( var rule in _rulesByTag)
            {
                if (string.Equals(item.Tag.DictionaryEntry.Keyword, rule.Tag?.DictionaryEntry.Keyword, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(item.ValueRepresentation.Code, rule.VR?.Code, StringComparison.InvariantCultureIgnoreCase)
                || (rule.IsMasked && rule.MaskedTag.IsMatch(item.Tag)))
                {
                    return rule;
                }
            }

            return null;
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
    }
}
