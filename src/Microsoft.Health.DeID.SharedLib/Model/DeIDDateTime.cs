// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.DeID.SharedLib.Model
{
    public class DeIDDateTime
    {
        public DeIDDateTime(string input)
        {
            var dateTime = DateTimeOffset.Parse(input);
            if (input.Contains(dateTime.Year.ToString()))
            {
                var yearIndex = input.IndexOf(dateTime.Year.ToString());
                input.Remove(yearIndex, dateTime.Year.ToString().Length);
            }
            else
            {
                return;
            }
        }

        public DateTimeOffset DateTimeOffset { get; set; }

        public int? Year { get; set; }

        public int? Month { get; set; }

        public int? Day { get; set; }
    }
}
