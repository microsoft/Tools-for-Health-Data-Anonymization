// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Microsoft.Health.Dicom.DeID.SharedLib.Model;
using Xunit;

namespace De.ID.Function.Shared.UnitTests
{
    public class PerturbTests
    {
        public static IEnumerable<object[]> GetStringValueToPerturb()
        {
            yield return new object[] { "5", new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 5, 5 };
            yield return new object[] { "5.0", new PerturbSetting() { Span = 0, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 5, 5 };
            yield return new object[] { "5.234", new PerturbSetting() { Span = 6, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 2.23, 8.23 };
            yield return new object[] { "5", new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5, 5 };
            yield return new object[] { "5.0", new PerturbSetting() { Span = 0.4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 4, 6 };
            yield return new object[] { "5.234", new PerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 2.67, 7.85 };
        }

        public static IEnumerable<object[]> GetDecimalToPerturb()
        {
            yield return new object[] { 5M, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 5M, 5M };
            yield return new object[] { 5.0M, new PerturbSetting() { Span = 0, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 5M, 5M };
            yield return new object[] { 5.234M, new PerturbSetting() { Span = 6, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 2.23M, 8.23M };
            yield return new object[] { 12e-2M, new PerturbSetting() { Span = 2, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, -0.88M, 1.12M };
            yield return new object[] { 5M, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5M, 5M };
            yield return new object[] { 5.0M, new PerturbSetting() { Span = 0.4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 4M, 6M };
            yield return new object[] { 5.234M, new PerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 2.67M, 7.85M };
            yield return new object[] { 12e-2M, new PerturbSetting() { Span = 4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, -0.12M, 0.36M };
        }

        public static IEnumerable<object[]> GetDoubleToPerturb()
        {
            yield return new object[] { 5D, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 5D, 5D };
            yield return new object[] { 5.0D, new PerturbSetting() { Span = 0, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 5D, 5D };
            yield return new object[] { 5.234D, new PerturbSetting() { Span = 6, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 2.23D, 8.23D };
            yield return new object[] { 12e-2D, new PerturbSetting() { Span = 2, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, -0.88D, 1.12D };
            yield return new object[] { 5D, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5D, 5D };
            yield return new object[] { 5.0D, new PerturbSetting() { Span = 0.4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 4D, 6D };
            yield return new object[] { 5.234D, new PerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 2.67D, 7.85D };
            yield return new object[] { 12e-2D, new PerturbSetting() { Span = 4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, -0.12D, 0.36D };
        }

        public static IEnumerable<object[]> GetFloatToPerturb()
        {
            yield return new object[] { 5F, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 5F, 5F };
            yield return new object[] { 5.0F, new PerturbSetting() { Span = 0, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 5F, 5F };
            yield return new object[] { 5.234F, new PerturbSetting() { Span = 6, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 2.23F, 8.23F };
            yield return new object[] { 12e-2F, new PerturbSetting() { Span = 2, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, -0.88F, 1.12F };
            yield return new object[] { 5F, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5F, 5F };
            yield return new object[] { 5.0F, new PerturbSetting() { Span = 0.4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 4F, 6F };
            yield return new object[] { 5.234F, new PerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 2.67F, 7.85F };
            yield return new object[] { 12e-2F, new PerturbSetting() { Span = 4, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, -0.12F, 0.36F };
        }

        public static IEnumerable<object[]> GetIntegerToPerturb()
        {
            yield return new object[] { 5, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 5, 5 };
            yield return new object[] { 10, new PerturbSetting() { Span = 4, RoundTo = 1, RangeType = PerturbRangeType.Fixed }, 8, 12 };
            yield return new object[] { 5e2, new PerturbSetting() { Span = 100, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 450, 550 };
            yield return new object[] { 5, new PerturbSetting() { Span = 0.1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5, 5 };
            yield return new object[] { 10, new PerturbSetting() { Span = 1, RoundTo = 1, RangeType = PerturbRangeType.Proportional }, 5, 15 };
            yield return new object[] { 5e2, new PerturbSetting() { Span = 0.5, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 375, 625 };
        }

        public static IEnumerable<object[]> GetUnsignedIntegerToPerturb()
        {
            yield return new object[] { 5U, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 5U, 5U };
            yield return new object[] { 0U, new PerturbSetting() { Span = 4, RoundTo = 1, RangeType = PerturbRangeType.Fixed }, 0U, 2U };
            yield return new object[] { (uint)5e2, new PerturbSetting() { Span = 100, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 450U, 550U };
            yield return new object[] { 5U, new PerturbSetting() { Span = 0.1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5U, 5U };
            yield return new object[] { 10U, new PerturbSetting() { Span = 4, RoundTo = 1, RangeType = PerturbRangeType.Proportional }, 0U, 30U };
            yield return new object[] { (uint)5e2, new PerturbSetting() { Span = 0.5, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 375U, 625U };
        }

        public static IEnumerable<object[]> GetAgeValueToPerturb()
        {
            yield return new object[] { new AgeValue(10, AgeType.Day), new PerturbSetting() { Span = 1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 5, 15 };
            yield return new object[] { new AgeValue(50, AgeType.Month), new PerturbSetting() { Span = 20, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, 40, 60 };
            yield return new object[] { new AgeValue(25, AgeType.Year), new PerturbSetting() { Span = 2, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 0, 50 };
        }

        public static IEnumerable<object[]> GetShortIntegerToPerturb()
        {
            yield return new object[] { (short)100, new PerturbSetting() { Span = 100, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, (short)50, (short)150 };
            yield return new object[] { (short)0, new PerturbSetting() { Span = 4, RoundTo = 1, RangeType = PerturbRangeType.Fixed }, (short)-2, (short)2 };
            yield return new object[] { (short)5e2, new PerturbSetting() { Span = 0.5, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, (short)375, (short)625 };
        }

        public static IEnumerable<object[]> GetUnsignedShortIntegerToPerturb()
        {
            yield return new object[] { (ushort)5, new PerturbSetting() { Span = 0, RoundTo = 0, RangeType = PerturbRangeType.Fixed }, (ushort)5, (ushort)5 };
            yield return new object[] { (ushort)0, new PerturbSetting() { Span = 4, RoundTo = 1, RangeType = PerturbRangeType.Fixed }, (ushort)0, (ushort)2 };
            yield return new object[] { (ushort)5e2, new PerturbSetting() { Span = 100, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, (ushort)450, (ushort)550 };
        }

        public static IEnumerable<object[]> GetLongToPerturb()
        {
            yield return new object[] { (long)int.MaxValue, new PerturbSetting() { Span = 1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, (long)int.MaxValue / 2, (long)int.MaxValue / 2 * 3 };
            yield return new object[] { 10L, new PerturbSetting() { Span = 4, RoundTo = 1, RangeType = PerturbRangeType.Fixed }, 8L, 12L };
            yield return new object[] { (long)5e2, new PerturbSetting() { Span = 100, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 450L, 550L };
        }

        public static IEnumerable<object[]> GetUnsignedLongToPerturb()
        {
            yield return new object[] { (ulong)int.MaxValue, new PerturbSetting() { Span = 1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, (long)int.MaxValue / 2, (long)int.MaxValue / 2 * 3 };
            yield return new object[] { 10UL, new PerturbSetting() { Span = 100, RoundTo = 1, RangeType = PerturbRangeType.Fixed }, 0UL, 60UL };
            yield return new object[] { (ulong)5e2, new PerturbSetting() { Span = 100, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 450UL, 550UL };
        }

        [Theory]
        [MemberData(nameof(GetStringValueToPerturb))]
        public void GivenAValueString_WhenPerturb_PerturbedValueShouldBeReturned(string value, PerturbSetting perturbSetting, decimal lowerBound, decimal upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.IsType<string>(result);
            Assert.InRange(decimal.Parse(result), lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(decimal.Parse(result)) <= perturbSetting.RoundTo);
        }

        [Theory]
        [MemberData(nameof(GetAgeValueToPerturb))]
        public void GivenAgeValue_WhenPerturb_PerturbedValueShouldBeReturned(AgeValue value, PerturbSetting perturbSetting, uint lowerBound, uint upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result.Age, lowerBound, upperBound);
            Assert.Equal(value.AgeType, value.AgeType);
        }

        [Theory]
        [MemberData(nameof(GetDecimalToPerturb))]
        public void GivenADecimal_WhenPerturb_PerturbedValueShouldBeReturned(decimal value, PerturbSetting perturbSetting, decimal lowerBound, decimal upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<decimal>(result);
        }

        [Theory]
        [MemberData(nameof(GetDoubleToPerturb))]
        public void GivenADouble_WhenPerturb_PerturbedValueShouldBeReturned(double value, PerturbSetting perturbSetting, double lowerBound, double upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces((decimal)result) <= perturbSetting.RoundTo);
            Assert.IsType<double>(result);
        }

        [Theory]
        [MemberData(nameof(GetFloatToPerturb))]
        public void GivenAFloat_WhenPerturb_PerturbedValueShouldBeReturned(float value, PerturbSetting perturbSetting, float lowerBound, float upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces((decimal)result) <= perturbSetting.RoundTo);
            Assert.IsType<float>(result);
        }

        [Theory]
        [MemberData(nameof(GetIntegerToPerturb))]
        public void GivenAnInteger_WhenPerturb_PerturbedValueShouldBeReturned(int value, PerturbSetting perturbSetting, int lowerBound, int upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<int>(result);
        }

        [Theory]
        [MemberData(nameof(GetUnsignedIntegerToPerturb))]
        public void GivenAnUnsignedInteger_WhenPerturb_PerturbedValueShouldBeReturned(uint value, PerturbSetting perturbSetting, uint lowerBound, uint upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<uint>(result);
        }

        [Theory]
        [MemberData(nameof(GetIntegerToPerturb))]
        public void GivenShortInteger_WhenPerturb_PerturbedValueShouldBeReturned(short value, PerturbSetting perturbSetting, short lowerBound, short upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<short>(result);
        }

        [Theory]
        [MemberData(nameof(GetUnsignedIntegerToPerturb))]
        public void GivenAnUnsignedShortInteger_WhenPerturb_PerturbedValueShouldBeReturned(ushort value, PerturbSetting perturbSetting, ushort lowerBound, ushort upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<ushort>(result);
        }

        [Theory]
        [MemberData(nameof(GetIntegerToPerturb))]
        public void GivenALongInteger_WhenPerturb_PerturbedValueShouldBeReturned(long value, PerturbSetting perturbSetting, long lowerBound, long upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<long>(result);
        }

        [Theory]
        [MemberData(nameof(GetUnsignedIntegerToPerturb))]
        public void GivenAnUnsignedLongInteger_WhenPerturb_PerturbedValueShouldBeReturned(ulong value, PerturbSetting perturbSetting, ulong lowerBound, ulong upperBound)
        {
            var result = PerturbFunction.Perturb(value, perturbSetting);
            Assert.InRange(result, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(result) <= perturbSetting.RoundTo);
            Assert.IsType<ulong>(result);
        }

        private int GetDecimalPlaces(decimal n)
        {
            n = Math.Abs(n);
            n -= (int)n;
            var decimalPlaces = 0;
            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }

            return decimalPlaces;
        }
    }
}
