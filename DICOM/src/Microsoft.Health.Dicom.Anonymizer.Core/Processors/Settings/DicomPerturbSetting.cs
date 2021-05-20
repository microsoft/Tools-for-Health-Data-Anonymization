// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings
{
    public class DicomPerturbSetting : PerturbSetting, IDicomAnonymizationSetting
    {
        public PerturbDistribution Distribution { get; set; } = PerturbDistribution.Uniform;

        public IDicomAnonymizationSetting CreateFromRuleSettings(string settings)
        {
            try
            {
                return JsonConvert.DeserializeObject<DicomPerturbSetting>(settings);
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Fail to parse perturb setting", ex);
            }
        }

        public IDicomAnonymizationSetting CreateFromRuleSettings(JObject settings)
        {
            try
            {
                return settings.ToObject<DicomPerturbSetting>();
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Fail to parse perturb setting", ex);
            }
        }

        public new void Validate()
        {
            try
            {
                base.Validate();
            }
            catch (Exception ex)
            {
                throw new AnonymizationConfigurationException(DicomAnonymizationErrorCode.InvalidRuleSettings, "Invalid perturb settings", ex);
            }
        }
    }
}
