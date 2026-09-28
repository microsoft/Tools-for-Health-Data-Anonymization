// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Text;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            ValidateConfiguration(configuration);
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

        private static void ValidateConfiguration(AnonymizerConfiguration configuration)
        {
            if (configuration.RuleContent == null)
            {
                return;
            }

            for (var index = 0; index < configuration.RuleContent.Length; index++)
            {
                var rule = configuration.RuleContent[index];
                var selector = rule?[Constants.TagKey]?.ToString()?.Trim();
                var method = rule?[Constants.MethodKey]?.ToString();
                if (string.IsNullOrWhiteSpace(selector) ||
                    string.Equals(method, nameof(AnonymizerMethod.Keep), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsInvariantUidSelector(selector))
                {
                    throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, $"Invariant UID selector '{selector}' at rule index {index} may only use keep.");
                }
            }
        }

        private static string NormalizeSelector(string selector)
        {
            return new string(selector.Where(character => !char.IsWhiteSpace(character)).ToArray());
        }

        private static bool IsInvariantUidSelector(string selector)
        {
            var normalized = NormalizeTagCharacters(selector);
            if (string.Equals(normalized, DicomVR.UI.Code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                var maskedTag = DicomMaskedTag.Parse(selector);
                if (maskedTag.IsMatch(DicomTag.SOPClassUID) ||
                    maskedTag.IsMatch(DicomTag.MediaStorageSOPClassUID) ||
                    maskedTag.IsMatch(DicomTag.TransferSyntaxUID))
                {
                    return true;
                }
            }
            catch (DicomDataException)
            {
            }

            return string.Equals(normalized, "SOPClassUID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "MediaStorageSOPClassUID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TransferSyntaxUID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "00080016", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "00020002", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "00020010", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeTagCharacters(string selector)
        {
            return NormalizeSelector(selector)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace(",", string.Empty);
        }
    }
}
