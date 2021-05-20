// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings
{
    public class DicomDateShiftSetting : DateShiftSetting, IDicomAnonymizationSetting
    {
        public DateShiftScope DateShiftScope { get; set; } = DateShiftScope.SopInstance;

        public IDicomAnonymizationSetting CreateFromRuleSettings(string settings)
        {
            try
            {
                return JsonConvert.DeserializeObject<DicomDateShiftSetting>(settings);
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Fail to parse dateshift setting", ex);
            }
        }

        public IDicomAnonymizationSetting CreateFromRuleSettings(JObject settings)
        {
            try
            {
                return settings.ToObject<DicomDateShiftSetting>();
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Fail to parse dateshift setting", ex);
            }
        }

        public void Validate()
        {
        }
    }
}
