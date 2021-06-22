// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.DeID.SharedLib.Models
{
    public class AgeObject
    {
        public AgeObject(uint age, AgeType ageType)
        {
            Value = age;
            AgeType = ageType;
        }

        public uint Value { get; } = 0;

        public AgeType AgeType { get; }

        public decimal? AgeInYears()
        {
            return AgeType switch
            {
                AgeType.Year => Value,
                AgeType.Month => Value / 12,
                AgeType.Week => Value / 52,
                AgeType.Day => Value / 365,
                _ => null,
            };
        }
    }
}
