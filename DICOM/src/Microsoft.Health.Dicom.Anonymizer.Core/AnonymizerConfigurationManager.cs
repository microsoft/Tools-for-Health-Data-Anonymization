// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Extensions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json;

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

        /// <summary>
        /// Creates an AnonymizerConfigurationManager from an IConfiguration instance.
        /// This enables support for multiple configuration sources (files, environment variables, etc.).
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">Optional section name to bind from. If null, binds from root.</param>
        /// <returns>A new AnonymizerConfigurationManager instance.</returns>
        public static AnonymizerConfigurationManager CreateFromConfiguration(IConfiguration configuration, string sectionName = null)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            
            var config = configuration.GetAnonymizerConfiguration(sectionName);
            return new AnonymizerConfigurationManager(config);
        }

        /// <summary>
        /// Creates an AnonymizerConfigurationManager from a configuration file using IConfiguration.
        /// This method supports the standard .NET configuration system.
        /// </summary>
        /// <param name="jsonFilePath">Path to the configuration file.</param>
        /// <returns>A new AnonymizerConfigurationManager instance.</returns>
        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string jsonFilePath)
        {
            EnsureArg.IsNotNull(jsonFilePath, nameof(jsonFilePath));
            
            var config = Microsoft.Health.Dicom.Anonymizer.Core.Extensions.ConfigurationExtensions.CreateAnonymizerConfigurationFromFile(jsonFilePath);
            return new AnonymizerConfigurationManager(config);
        }
    }
}
