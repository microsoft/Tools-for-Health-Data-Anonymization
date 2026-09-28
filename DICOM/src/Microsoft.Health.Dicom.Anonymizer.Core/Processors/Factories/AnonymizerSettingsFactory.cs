// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class AnonymizerSettingsFactory
    {
        public T CreateAnonymizerSetting<T>(JObject settings)
        {
            EnsureArg.IsNotNull(settings, nameof(settings));

            try
            {
                return settings.ToObject<T>();
            }
            catch (JsonException)
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Failed to parse rule setting.");
            }
        }
    }
}
