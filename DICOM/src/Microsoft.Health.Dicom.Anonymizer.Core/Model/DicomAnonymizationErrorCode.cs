﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.Model
{
    public enum DicomAnonymizationErrorCode
    {
        MissingConfigurationFields,
        InvalidConfigurationValues,
        UnsupportedAnonymizationRule,
        MissingRuleSettings,
        InvalidRuleSettings,

        UnsupportedAnonymizationMethod,
    }
}
