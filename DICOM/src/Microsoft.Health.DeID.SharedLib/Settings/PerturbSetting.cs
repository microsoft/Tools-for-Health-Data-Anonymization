// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class PerturbSetting
    {
        public double Span { get; set; } = 1;

        public PerturbRangeType RangeType { get; set; } = PerturbRangeType.Proportional;

        public int RoundTo { get; set; } = 2;

        public Func<double, double> NoiseFunction { get; set; }

        public void Validate()
        {
            if (Span < 0 || RoundTo > 28 || RoundTo < 0)
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.InvalidDeIdSettings, "Perturb setting is invalid.");
            }
        }
    }
}
