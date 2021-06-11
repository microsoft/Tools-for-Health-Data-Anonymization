// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
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

        public static AnonymizerConfigurationManager CreateFromJson(string json)
        {
            EnsureArg.IsNotNull(json, nameof(json));
            try
            {
                var configuration = JsonConvert.DeserializeObject<AnonymizerConfiguration>(json);
                return new AnonymizerConfigurationManager(configuration);
            }
            catch (JsonException innerException)
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.ParsingJsonConfigurationFailed, $"Failed to parse configuration file", innerException);
            }
        }

        public static AnonymizerConfigurationManager CreateFromJsonFile(string jsonFilePath)
        {
            EnsureArg.IsNotNull(jsonFilePath, nameof(jsonFilePath));

            var content = File.ReadAllText(jsonFilePath, Encoding.UTF8);
            return CreateFromJson(content);
        }
    }
}
