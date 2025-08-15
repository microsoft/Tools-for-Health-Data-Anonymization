// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    /// <summary>
    /// Extension methods for IConfiguration to support anonymizer configuration loading.
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

                // Handle special case for FhirPathRules that need to be converted from JSON
                var fhirPathRulesSection = section.GetSection("fhirPathRules");
                if (fhirPathRulesSection.Exists())
                {
                    // Try to get as object array first and then convert to Dictionary<string, object>[]
                    var rulesJson = fhirPathRulesSection.Get<object[]>();
                    if (rulesJson != null)
                    {
                        config.FhirPathRules = new Dictionary<string, object>[rulesJson.Length];
                        for (int i = 0; i < rulesJson.Length; i++)
                        {
                            if (rulesJson[i] is Dictionary<string, object> dict)
                            {
                                config.FhirPathRules[i] = dict;
                            }
                            else
                            {
                                // Convert from JObject or other formats
                                var jsonString = JsonConvert.SerializeObject(rulesJson[i]);
                                config.FhirPathRules[i] = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                            }
                        }
                    }
                }

                // Bind ParameterConfiguration
                var parametersSection = section.GetSection("parameters");
                if (parametersSection.Exists())
                {
                    config.ParameterConfiguration = parametersSection.Get<ParameterConfiguration>();
                }

                return config;
            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationException("Failed to bind configuration to AnonymizerConfiguration", ex);
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
                throw new AnonymizerConfigurationException($"Configuration file not found: {jsonFilePath}");
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
                throw new AnonymizerConfigurationException("Failed to parse JSON configuration", ex);
            }
        }
    }
}