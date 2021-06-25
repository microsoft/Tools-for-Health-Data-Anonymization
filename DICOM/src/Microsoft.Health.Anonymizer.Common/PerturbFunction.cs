// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Models;

namespace Microsoft.Health.Anonymizer.Common
{
    public class PerturbFunction
    {
        private readonly PerturbSetting _perturbSetting;

        public PerturbFunction(PerturbSetting perturbSetting)
        {
            EnsureArg.IsNotNull(perturbSetting, nameof(perturbSetting));

            _perturbSetting = perturbSetting ?? new PerturbSetting();
            _perturbSetting.Validate();
        }

        public AgeObject Perturb(AgeObject value)
        {
            EnsureArg.IsNotNull(value, nameof(value));

            return new AgeObject(Perturb(value.Value), value.AgeType);
        }

        public decimal Perturb(decimal value)
        {
            var noise = (decimal)GenerateNoise((double)value);
            return Math.Round(value + noise, _perturbSetting.RoundTo);
        }

        public string Perturb(string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));

            if (decimal.TryParse(value, out decimal originValue))
            {
                return Perturb(originValue).ToString();
            }
            else
            {
                throw new AnonymizerException(AnonymizerErrorCode.PerturbFailed, "The input value is not a numeric value that can not be perturbed.");
            }
        }

        public double Perturb(double value)
        {
            return Math.Round(value + GenerateNoise(value), _perturbSetting.RoundTo);
        }

        public float Perturb(float value)
        {
            return (float)Math.Round(value + GenerateNoise(value), _perturbSetting.RoundTo);
        }

        public int Perturb(int value)
        {
            return (int)Math.Round(value + GenerateNoise(value), 0);
        }

        public short Perturb(short value)
        {
            return (short)Math.Round(value + GenerateNoise(value), 0);
        }

        public long Perturb(long value)
        {
            return (long)Math.Round(value + GenerateNoise(value), 0);
        }

        public uint Perturb(uint value)
        {
            var noise = GenerateNoise(value);
            return (uint)Math.Round(Math.Max(value + noise, 0), 0);
        }

        public ushort Perturb(ushort value)
        {
            var noise = GenerateNoise(value);

            return (ushort)Math.Round(Math.Max(value + noise, 0), 0);
        }

        public ulong Perturb(ulong value)
        {
            var noise = GenerateNoise(value);
            return (ulong)Math.Round(Math.Max(value + noise, 0), 0);
        }

        private double GenerateNoise(double value)
        {
            var span = _perturbSetting.Span;
            if (_perturbSetting.RangeType == PerturbRangeType.Proportional)
            {
                span = Math.Abs(value * _perturbSetting.Span);
            }

            return _perturbSetting.NoiseFunction == null ? ContinuousUniform.Sample(-1 * span / 2, span / 2) : _perturbSetting.NoiseFunction(span);
        }
    }
}
