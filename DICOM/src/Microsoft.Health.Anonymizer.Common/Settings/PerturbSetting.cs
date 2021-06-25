// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Anonymizer.Common.Exceptions;

namespace Microsoft.Health.Anonymizer.Common
{
    public class PerturbSetting
    {
        private const int MaxRoundToValue = 28;

        public double Span { get; set; } = 1;

        public PerturbRangeType RangeType { get; set; } = PerturbRangeType.Proportional;

        public int RoundTo { get; set; } = 2;

        public Func<double, double> NoiseFunction { get; set; }

        public void Validate()
        {
            if (Span < 0 || RoundTo > MaxRoundToValue || RoundTo < 0)
            {
                throw new AnonymizerException(
                    AnonymizerErrorCode.InvalidAnonymizerSettings,
                    "Perturb setting is invalid. \r\n1. Span must be greater than 0. \r\n2. RoundTo value must between 0 and 28.");
            }
        }
    }
}
