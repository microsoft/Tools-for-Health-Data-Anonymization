// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;
using Xunit;

namespace De.ID.Function.Shared.UnitTests
{
    public class DateShiftTests
    {
        public static IEnumerable<object[]> GetDateStringForDateShift()
        {
            yield return new object[] { "2015-02-07", DateTime.Parse("2014-12-19"), DateTime.Parse("2015-03-29") };
            yield return new object[] { "2020-01-17", DateTime.Parse("2019-11-28"), DateTime.Parse("2020-03-07") };
            yield return new object[] { "1998-10-02", DateTime.Parse("1998-08-13"), DateTime.Parse("1998-11-21") };
            yield return new object[] { "1975-12-26", DateTime.Parse("1975-11-06"), DateTime.Parse("1976-02-14") };
        }

        public static IEnumerable<object[]> GetDateStringForDateShiftWithPrefix()
        {
            yield return new object[] { "2015-02-07", "1975-11-06" };
            yield return new object[] { "2020-01-17", "1998-11-21" };
            yield return new object[] { "1998-10-02", "2019-11-28" };
            yield return new object[] { "1975-12-26", "2020-03-07" };
        }

        public static IEnumerable<object[]> GetDateTimeStringForDateShift()
        {
            yield return new object[] { "2015-02-07", DateTimeOffset.Parse("2014-12-19"), DateTimeOffset.Parse("2015-03-29") };
            yield return new object[] { "2015-02-07T13:28:17-05:00", DateTimeOffset.Parse("2014-12-19T00:00:00-05:00"), DateTimeOffset.Parse("2015-03-29T00:00:00-05:00") };
            yield return new object[] { "1998-10-02", DateTimeOffset.Parse("1998-08-13"), DateTimeOffset.Parse("1998-11-21") };
            yield return new object[] { "1998-10-02T08:47:25+08:00", DateTimeOffset.Parse("1998-08-13T00:00:00+08:00"), DateTimeOffset.Parse("1998-11-21T00:00:00+08:00") };
        }

        public static IEnumerable<object[]> GetDateTimeStringForDateShiftFormatTest()
        {
            yield return new object[] { "dummy", "2015-02-07", "2015-01-17T00:00:00+08:00" };  // mark
            yield return new object[] { "dummy", "2015-02-07T13:28:17-05:00", "2015-01-17T00:00:00-05:00" };
            yield return new object[] { "dummy", "2015-02-07T13:28:17+05:00", "2015-01-17T00:00:00+05:00" };
            yield return new object[] { "dummy", "2015-02-07T13:28:17Z", "2015-01-17T00:00:00+00:00" }; // mark
            yield return new object[] { "dummy", "2015-02-07T13:28:17.12345-05:00", "2015-01-17T00:00:00-05:00" };
        }

        public static IEnumerable<object[]> GetDateTimeForDateShift()
        {
            yield return new object[] { DateTimeOffset.Parse("2015-02-07"), DateTimeOffset.Parse("2014-12-19"), DateTimeOffset.Parse("2015-03-29") };
            yield return new object[] { DateTimeOffset.Parse("2015-02-07T13:28:17-05:00"), DateTimeOffset.Parse("2014-12-19T00:00:00-05:00"), DateTimeOffset.Parse("2015-03-29T00:00:00-05:00") };
            yield return new object[] { DateTimeOffset.Parse("1998-10-02"), DateTimeOffset.Parse("1998-08-13"), DateTimeOffset.Parse("1998-11-21") };
            yield return new object[] { DateTimeOffset.Parse("1998-10-02T08:47:25+08:00"), DateTimeOffset.Parse("1998-08-13T00:00:00+08:00"), DateTimeOffset.Parse("1998-11-21T00:00:00+08:00") };
        }

        [Theory]
        [MemberData(nameof(GetDateStringForDateShift))]
        public void GivenADate_WhenDateShift_ThenDateShouldBeShifted(string date, DateTime minExpectedDate, DateTime maxExpectedDate)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = string.Empty });
            var processResult = dateShiftFunction.ShiftDate(date);

            Assert.True(minExpectedDate <= DateTime.Parse(processResult));
            Assert.True(maxExpectedDate >= DateTime.Parse(processResult));
        }

        [Theory]
        [MemberData(nameof(GetDateStringForDateShiftWithPrefix))]
        public void GivenADate_WhenDateShiftWithSamePrefix_ThenSameAmountShouldBeShifted(string date1, string date2)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = "123", DateShiftKeyPrefix = "filename" });
            var processResult1 = dateShiftFunction.ShiftDate(date1);
            var offset1 = DateTime.Parse(processResult1).Subtract(DateTime.Parse(date1.ToString()));

            var processResult2 = dateShiftFunction.ShiftDate(date2);
            var offset2 = DateTime.Parse(processResult2).Subtract(DateTime.Parse(date2.ToString()));

            Assert.Equal(offset1.Days, offset2.Days);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeStringForDateShift))]
        public void GivenADateTimeString_WhenDateShift_ThenDateTimeShouldBeShifted(string dateTime, DateTimeOffset minExpectedDateTime, DateTimeOffset maxExpectedDateTime)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = Guid.NewGuid().ToString("N") });
            var processResult = dateShiftFunction.ShiftDateTime(dateTime);

            Assert.True(minExpectedDateTime <= DateTimeOffset.Parse(processResult));
            Assert.True(maxExpectedDateTime >= DateTimeOffset.Parse(processResult));
        }

        [Theory]
        [MemberData(nameof(GetDateTimeStringForDateShiftFormatTest))]
        public void GivenADateTime_WhenDateShift_ThenDateTimeFormatShouldNotChange(string dateShiftKey, string dateTime, string expectedDateTimeString)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = dateShiftKey });
            var processResult = dateShiftFunction.ShiftDateTime(dateTime);
            Assert.Equal(expectedDateTimeString, processResult);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeForDateShift))]
        public void GivenADateTime_WhenDateShift_ThenDateTimeShouldBeShifted(DateTimeOffset dateTime, DateTimeOffset minExpectedDateTime, DateTimeOffset maxExpectedDateTime)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = Guid.NewGuid().ToString("N") });
            var processResult = dateShiftFunction.ShiftDateTime(dateTime);

            Assert.True(minExpectedDateTime <= processResult);
            Assert.True(maxExpectedDateTime >= processResult);
        }
    }
}
