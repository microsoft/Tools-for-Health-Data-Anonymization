// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;
using Xunit;

namespace De.ID.Function.Shared.UnitTests
{
    public class RedactTests
    {
        public static IEnumerable<object[]> GetDateDataForPartialRedact()
        {
            yield return new object[] { "2015", "2015" };
            yield return new object[] { "2015-02", "2015" };
            yield return new object[] { "2015-02-07", "2015" };
            yield return new object[] { "1925-02-07", null };
        }

        public static IEnumerable<object[]> GetDateDataWithFormatForPartialRedact()
        {
            yield return new object[] { "2015", "yyyyMMddhhmmss.ffffff", "2015" };
            yield return new object[] { "201502", "yyyyMMddhhmmss.ffffff", "2015" };
            yield return new object[] { "20150207", "yyyyMMddhhmmss.ffffff", "2015" };
            yield return new object[] { "19250207121212.12", "yyyyMMddhhmmss.ffffff", null };
        }

        public static IEnumerable<object[]> GetDateDataForRedact()
        {
            yield return new object[] { "2015" };
            yield return new object[] { "2015-02" };
            yield return new object[] { "2015-02-07" };
            yield return new object[] { "1925-02-07" };
        }

        public static IEnumerable<object[]> GetDateTimeDataForRedact()
        {
            yield return new object[] { "2015", "2015" };
            yield return new object[] { "2015-02", "2015" };
            yield return new object[] { "2015-02-07", "2015" };
            yield return new object[] { "2015-02-07T13:28:17-05:00", "2015" };
            yield return new object[] { "1925-02-07T13:28:17-05:00", null };
        }

        public static IEnumerable<object[]> GetInstantDataForRedact()
        {
            yield return new object[] { "2015-02-07T13:28:17-05:00", "2015" };
            yield return new object[] { "1925-02-07T13:28:17-05:00", null };
        }

        public static IEnumerable<object[]> GetAgeDataForPartialRedact()
        {
            yield return new object[] { 92 };
            yield return new object[] { 57 };
        }

        public static IEnumerable<object[]> GetAgeDataForRedact()
        {
            yield return new object[] { "101" };
            yield return new object[] { "35" };
        }

        public static IEnumerable<object[]> GetPostalCodeDataForRedact()
        {
            yield return new object[] { "98052" };
            yield return new object[] { "10104" };
            yield return new object[] { "00000" };
            yield return new object[] { "98028-1830" };
        }

        public static IEnumerable<object[]> GetPostalCodeDataForPartialRedact()
        {
            yield return new object[] { "98052", "98000" };
            yield return new object[] { "10104", "10100" };
            yield return new object[] { "20301", "00000" };
            yield return new object[] { "55602", "00000" };
            yield return new object[] { "98028-1830", "98000-0000" };
            yield return new object[] { "20301-1830", "00000-0000" };
        }

        [Theory]
        [MemberData(nameof(GetPostalCodeDataForRedact))]
        public void GivenAPostalCode_WhenRedact_ThenDigitsShouldBeRedacted(string postalCode)
        {
            var redactFunction = new RedactFunction(new RedactSetting());
            var processResult = redactFunction.RedactPostalCode(postalCode);
            Assert.Null(processResult);
        }

        [Theory]
        [MemberData(nameof(GetPostalCodeDataForPartialRedact))]
        public void GivenAPostalCode_WhenPartialRedact_ThenPartialDigitsShouldBeRedacted(string postalCode, string expectedPostalCode)
        {
            var redactFunction = new RedactFunction(new RedactSetting() { EnablePartialZipCodesForRedact = true, RestrictedZipCodeTabulationAreas = new List<string>() { "203", "556" } });
            var processResult = redactFunction.RedactPostalCode(postalCode);
            Assert.Equal(expectedPostalCode.ToString(), processResult);
        }

        [Theory]
        [MemberData(nameof(GetDateDataWithFormatForPartialRedact))]
        public void GivenADateWithFormat_WhenPartialRedact_ThenDateShouldBeRedacted(string date, string format, string expectedDate)
        {
            var redactFunction = new RedactFunction(new RedactSetting() { EnablePartialDatesForRedact = true });
            var processResult = redactFunction.RedactDateTime(date, format);
            Assert.Equal(expectedDate ?? null, processResult);
        }

        [Theory]
        [MemberData(nameof(GetDateDataForPartialRedact))]
        public void GivenADate_WhenPartialRedact_ThenDateShouldBeRedacted(string date, string expectedDate)
        {
            var redactFunction = new RedactFunction(new RedactSetting() { EnablePartialDatesForRedact = true });
            var processResult = redactFunction.RedactDateTime(date);
            Assert.Equal(expectedDate ?? null, processResult);
        }

        [Theory]
        [MemberData(nameof(GetDateDataForRedact))]
        public void GivenADate_WhenRedact_ThenDateShouldBeRedacted(string date)
        {
            var redactFunction = new RedactFunction(new RedactSetting());
            var processResult = redactFunction.RedactDateTime(date);
            Assert.Null(processResult);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeDataForRedact))]
        public void GivenADateTime_WhenRedact_ThenDateTimeShouldBeRedacted(string dateTime, string expectedDateTime)
        {
            var redactFunction = new RedactFunction(new RedactSetting() { EnablePartialDatesForRedact = true });
            var processResult = redactFunction.RedactDateTime(dateTime);
            Assert.Equal(expectedDateTime ?? null, processResult);
        }

        [Theory]
        [MemberData(nameof(GetInstantDataForRedact))]
        public void GivenAnInstant_WhenRedact_ThenInstantShouldBeRedacted(string instant, string expectedInstantString)
        {
            var redactFunction = new RedactFunction(new RedactSetting() { EnablePartialDatesForRedact = true });
            var processResult = redactFunction.RedactDateTime(instant);
            Assert.Equal(expectedInstantString ?? null, processResult);
        }

        [Theory]
        [MemberData(nameof(GetAgeDataForPartialRedact))]
        public void GivenAnAge_WhenPartialRedact_ThenAgeOver89ShouldBeRedacted(int age)
        {
            var redactFunction = new RedactFunction(new RedactSetting() { EnablePartialAgeForRedact = true });
            var processResult = redactFunction.RedactAge(age);
            if (age > 89)
            {
                Assert.Null(processResult);
            }
            else
            {
                Assert.Equal(age, processResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetAgeDataForRedact))]
        public void GivenAnAge_WhenRedact_ThenAgeShouldBeRedacted(int age)
        {
            var redactFunction = new RedactFunction(new RedactSetting());
            var processResult = redactFunction.RedactAge(age);
            Assert.Null(processResult);
        }
    }
}
