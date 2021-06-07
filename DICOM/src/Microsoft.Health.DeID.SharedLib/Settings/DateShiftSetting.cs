// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;

namespace Microsoft.Health.Dicom.DeID.SharedLib.Settings
{
    public class DateShiftSetting
    {
        public uint DateShiftRange { get; set; } = 50;

        public string DateShiftKey { get; set; } = string.Empty;

        public string DateShiftKeyPrefix { get; set; } = string.Empty;
    }
}
