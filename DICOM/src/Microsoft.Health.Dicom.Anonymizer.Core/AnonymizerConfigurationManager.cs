// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfiguration _configuration;

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public AnonymizerConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
        }

        public static AnonymizerConfigurationManager CreateFromJsonConfiguration(string jsonConfiguration)
        {
            EnsureArg.IsNotNull(jsonConfiguration, nameof(jsonConfiguration));

            var configuration = JsonConvert.DeserializeObject<AnonymizerConfiguration>(jsonConfiguration);
            return new AnonymizerConfigurationManager(configuration);
        }

        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string configFilePath)
        {
            EnsureArg.IsNotNull(configFilePath, nameof(configFilePath));

            var content = File.ReadAllText(configFilePath);
            return CreateFromJsonConfiguration(content);
        }
    }
}
