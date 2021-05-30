// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfiguration _configuration;
        private readonly AnonymizerRuleFactory _ruleFactory;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration, IAnonymizerProcessorFactory processorFactory)
        {
            _configuration = configuration;
            _ruleFactory = new AnonymizerRuleFactory(_configuration, processorFactory);
        }

        public AnonymizerRule[] DicomRules { get; private set; } = null;

        public static AnonymizerConfigurationManager CreateFromSettingsInJson(string settingsInJson)
        {
            try
            {
                var configuration = JsonConvert.DeserializeObject<AnonymizerConfiguration>(settingsInJson);
                return new AnonymizerConfigurationManager(configuration, new DicomProcessorFactory());
            }
            catch (JsonException innerException)
            {
                throw new JsonException($"Failed to parse configuration file", innerException);
            }
        }

        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string configFilePath)
        {
            try
            {
                var content = File.ReadAllText(configFilePath);
                return CreateFromSettingsInJson(content);
            }
            catch (IOException innerException)
            {
                throw new IOException($"Failed to read configuration file {configFilePath}", innerException);
            }
        }

        public AnonymizerRule[] CreateAnonymizerRules()
        {
            return _configuration.DicomRules?.Select(entry => _ruleFactory.CreateAnonymizationDicomRule(entry)).ToArray();
        }
    }
}
