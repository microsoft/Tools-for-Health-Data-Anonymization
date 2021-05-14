// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfiguration _configuration;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _configuration = configuration;
            DicomTagRules = _configuration.DicomTagRules?.Select(entry => AnonymizerDicomTagRule.CreateAnonymizationDicomRule(entry, _configuration)).ToArray();
        }

        public AnonymizerDicomTagRule[] DicomTagRules { get; private set; } = null;

        public static AnonymizerConfigurationManager CreateFromSettingsInJson(string settingsInJson)
        {
            try
            {
                var configuration = JsonConvert.DeserializeObject<AnonymizerConfiguration>(settingsInJson);
                return new AnonymizerConfigurationManager(configuration);
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

        public AnonymizerDefaultSettings GetDefaultSettings()
        {
            return _configuration.DefaultSettings;
        }
    }
}
