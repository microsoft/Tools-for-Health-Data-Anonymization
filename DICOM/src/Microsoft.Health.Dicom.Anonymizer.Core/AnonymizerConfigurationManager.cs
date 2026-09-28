// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            Configuration = configuration;
        }

        public AnonymizerConfiguration Configuration { get; }

        public static AnonymizerConfigurationManager CreateFromJson(string json)
        {
            EnsureArg.IsNotNull(json, nameof(json));
            try
            {
                var root = JObject.Parse(json);
                var allowedFields = new[] { "rules", "defaultSettings", "customSettings" };
                foreach (var property in root.Properties())
                {
                    if (!System.Array.Exists(allowedFields, field => string.Equals(field, property.Name, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new AnonymizerConfigurationException(
                            DicomAnonymizationErrorCode.InvalidConfigurationValues,
                            "Policy validation failed: unknown top-level field.");
                    }
                }

                var configuration = root.ToObject<AnonymizerConfiguration>();
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
