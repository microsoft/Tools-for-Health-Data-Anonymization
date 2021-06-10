// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings
{
    public class AnonymizerSettingsFactory : IAnonymizerSettingsFactory
    {
        public object CreateAnonymizerSetting(Type settingType, JObject settingObject)
        {
            EnsureArg.IsNotNull(settingType, nameof(settingType));
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            try
            {
                return settingObject.ToObject(settingType);
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Fail to parse anonymizer setting.", ex);
            }
        }

        public T CreateAnonymizerSetting<T>(JObject settings)
        {
            EnsureArg.IsNotNull(settings, nameof(settings));

            try
            {
                return settings.ToObject<T>();
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Fail to parse anonymizer setting.", ex);
            }
        }
    }
}
