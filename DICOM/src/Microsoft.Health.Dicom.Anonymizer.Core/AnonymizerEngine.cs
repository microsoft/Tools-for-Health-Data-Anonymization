// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerSettings _anonymizerSettings;
        private readonly AnonymizerConfigurationManager _configurationManager;
        private readonly IAnonymizerRuleFactory _ruleFactory;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerSettings anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null)
            : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath), anonymizerSettings, ruleFactory)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerSettings anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null)
        {
            EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));

            _anonymizerSettings = anonymizerSettings ?? new AnonymizerSettings();
            _configurationManager = configurationManager;
            _ruleFactory = ruleFactory ?? new AnonymizerRuleFactory(_configurationManager.GetConfiguration(), new DicomProcessorFactory(), new AnonymizerSettingsFactory());
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public void AnonymizeDataset(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            var context = InitContext(dataset);
            ValidateInput(dataset);
            dataset.AutoValidate = _anonymizerSettings.AutoValidate;

            var rules = _ruleFactory.CreateAnonymizationDicomRule(_configurationManager.GetConfiguration().RuleContent);
            foreach (var rule in rules)
            {
                rule.Handle(dataset, context);
                _logger.LogDebug($"Successfully handle rule {rule.Description}.");
            }
        }

        private void ValidateInput(DicomDataset dataset)
        {
            if (_anonymizerSettings.ValidateInput)
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
