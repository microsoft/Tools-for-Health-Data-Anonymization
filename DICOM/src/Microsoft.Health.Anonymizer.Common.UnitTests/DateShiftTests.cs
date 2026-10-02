// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Health.Anonymizer.Common.Models;
using Microsoft.Health.Anonymizer.Common.Settings;
using Xunit;

namespace Microsoft.Health.Anonymizer.Common.UnitTests
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
            foreach (var (input, value) in GetDateTimeInputs())
            {
                foreach (var (key, shiftDays) in GetDateShiftBoundaries())
                {
                    yield return new object[] { input, value.DateTime, key, shiftDays };
                }
            }
        }

        public static IEnumerable<object[]> GetDateTimeForDateShift()
        {
            foreach (var (_, value) in GetDateTimeInputs())
            {
                foreach (var (key, shiftDays) in GetDateShiftBoundaries())
                {
                    yield return new object[] { value, key, shiftDays };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetDateStringForDateShift))]
        public void GivenADate_WhenDateShift_ThenDateShouldBeShifted(string date, DateTime minExpectedDate, DateTime maxExpectedDate)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = string.Empty });
            var processResult = dateShiftFunction.Shift(date, AnonymizerValueTypes.Date);

            Assert.True(minExpectedDate <= DateTime.Parse(processResult));
            Assert.True(maxExpectedDate >= DateTime.Parse(processResult));
        }

        [Theory]
        [MemberData(nameof(GetDateStringForDateShiftWithPrefix))]
        public void GivenADate_WhenDateShiftWithSamePrefix_ThenSameAmountShouldBeShifted(string date1, string date2)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting() { DateShiftKey = "123", DateShiftKeyPrefix = "filename" });
            var processResult1 = dateShiftFunction.Shift(date1, AnonymizerValueTypes.Date);
            var offset1 = DateTime.Parse(processResult1).Subtract(DateTime.Parse(date1));

            var processResult2 = dateShiftFunction.Shift(date2, AnonymizerValueTypes.Date);
            var offset2 = DateTime.Parse(processResult2).Subtract(DateTime.Parse(date2));

            Assert.Equal(offset1.Days, offset2.Days);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeStringForDateShift))]
        public void GivenADateTimeString_WhenDateShift_ThenDateTimeShouldBeShifted(string input, DateTime wallClock, string key, int shiftDays)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting { DateShiftKey = key, DateShiftRange = 50 });
            var processResult = dateShiftFunction.Shift(input, AnonymizerValueTypes.DateTime);
            var expected = wallClock.AddDays(shiftDays).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

            Assert.Equal(expected, processResult);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeForDateShift))]
        public void GivenADateTime_WhenDateShift_ThenDateTimeShouldBeShifted(DateTimeOffset dateTime, string key, int shiftDays)
        {
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting { DateShiftKey = key, DateShiftRange = 50 });
            var processResult = dateShiftFunction.Shift(dateTime);

            Assert.Equal(dateTime.AddDays(shiftDays), processResult);
            Assert.Equal(dateTime.TimeOfDay, processResult.TimeOfDay);
            Assert.Equal(dateTime.Offset, processResult.Offset);
        }

        private static IEnumerable<(string Key, int ShiftDays)> GetDateShiftBoundaries()
        {
            // With range 50, single-byte keys 50 and 100 select the midpoint and upper boundary.
            yield return (string.Empty, -50);
            yield return ("2", 0);
            yield return ("d", 50);
            yield return ("00000000000000000000000000000028", 50);
        }

        private static IEnumerable<(string Input, DateTimeOffset Value)> GetDateTimeInputs()
        {
            yield return ("2015-02-07", new DateTimeOffset(2015, 2, 7, 0, 0, 0, TimeSpan.Zero));
            yield return ("2015-02-07T13:28:17-05:00", new DateTimeOffset(2015, 2, 7, 13, 28, 17, TimeSpan.FromHours(-5)));
            yield return ("1998-10-02", new DateTimeOffset(1998, 10, 2, 0, 0, 0, TimeSpan.Zero));
            yield return ("1998-10-02T08:47:25+08:00", new DateTimeOffset(1998, 10, 2, 8, 47, 25, TimeSpan.FromHours(8)));
            yield return ("2024-03-01T23:59:59.123456+05:45", new DateTimeOffset(2024, 3, 1, 23, 59, 59, TimeSpan.FromMinutes(345)).AddTicks(1234560));
            yield return ("2024-03-01T00:00:00.000001+00:00", new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(10));
        }
    }
}
