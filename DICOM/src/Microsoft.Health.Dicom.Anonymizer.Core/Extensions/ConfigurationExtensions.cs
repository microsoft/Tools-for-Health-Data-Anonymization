// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Extensions
{
    /// <summary>
    /// Extension methods for IConfiguration to support DICOM anonymizer configuration loading.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Binds an IConfiguration to an AnonymizerConfiguration object.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">The section name to bind from. If null, binds from root.</param>
        /// <returns>A configured AnonymizerConfiguration instance.</returns>
        public static AnonymizerConfiguration GetAnonymizerConfiguration(this IConfiguration configuration, string sectionName = null)
        {
            try
            {
                var section = string.IsNullOrEmpty(sectionName) ? configuration : configuration.GetSection(sectionName);
                var config = new AnonymizerConfiguration();
                
                // Bind basic properties
                section.Bind(config);

                // Handle special case for RuleContent that need to be converted from JSON
                var rulesSection = section.GetSection("rules");
                if (rulesSection.Exists())
                {
                    var rulesJson = rulesSection.Get<JObject[]>();
                    config.RuleContent = rulesJson;
                }

                // Bind DefaultSettings
                var defaultSettingsSection = section.GetSection("defaultSettings");
                if (defaultSettingsSection.Exists())
                {
                    config.DefaultSettings = defaultSettingsSection.Get<AnonymizerDefaultSettings>();
                }

                // Bind CustomSettings 
                var customSettingsSection = section.GetSection("customSettings");
                if (customSettingsSection.Exists())
                {
                    var customSettingsJson = customSettingsSection.Get<JObject>();
                    if (customSettingsJson != null)
                    {
                        config.CustomSettings = customSettingsJson.ToObject<System.Collections.Generic.Dictionary<string, JObject>>();
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Failed to bind configuration to AnonymizerConfiguration", ex);
            }
        }

        /// <summary>
        /// Creates a configuration builder from a JSON file and binds it to AnonymizerConfiguration.
        /// </summary>
        /// <param name="jsonFilePath">Path to the JSON configuration file.</param>
        /// <returns>A configured AnonymizerConfiguration instance.</returns>
        public static AnonymizerConfiguration CreateAnonymizerConfigurationFromFile(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, $"Configuration file not found: {jsonFilePath}");
            }

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonFilePath, optional: false, reloadOnChange: false);

            var configuration = builder.Build();
            return configuration.GetAnonymizerConfiguration();
        }

        /// <summary>
        /// Creates a configuration builder from a JSON string and binds it to AnonymizerConfiguration.
        /// </summary>
        /// <param name="jsonContent">JSON content as string.</param>
        /// <returns>A configured AnonymizerConfiguration instance.</returns>
        public static AnonymizerConfiguration CreateAnonymizerConfigurationFromJson(string jsonContent)
        {
            try
            {
                // Parse JSON to verify it's valid and convert to configuration format
                var jsonObject = JObject.Parse(jsonContent);
                
                var tempFile = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(tempFile, jsonContent);
                    return CreateAnonymizerConfigurationFromFile(tempFile);
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.ParsingJsonConfigurationFailed, "Failed to parse JSON configuration", ex);
            }
        }
    }
}