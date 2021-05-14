// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.DeID.SharedLib.Model
{
    public class AgeValue
    {
        public AgeValue(uint age, AgeType ageType)
        {
            Age = age;
            AgeType = ageType;
        }

        public uint Age { get; } = 0;

        public AgeType AgeType { get; }

        public decimal? AgeToYearsOld()
        {
            return AgeType switch
            {
                AgeType.Year => Age,
                AgeType.Month => Age / 12,
                AgeType.Week => Age / 52,
                AgeType.Day => Age / 365,
                _ => null,
            };
        }
    }
}
