// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.DeID.SharedLib.Model
{
    public class DateTimeObject
    {
        public DateTimeOffset DateValue { get; set; }

        public bool? HasTimeZone { get; set; } = null;

        public bool? HasMillionSecond { get; set; } = null;

        public bool? HasSecond { get; set; } = null;

        public bool? HasHour { get; set; } = null;

        public bool? HasDay { get; set; } = null;

        public bool? HasMonth { get; set; } = null;

        public bool? HasYear { get; set; } = null;
    }
}
