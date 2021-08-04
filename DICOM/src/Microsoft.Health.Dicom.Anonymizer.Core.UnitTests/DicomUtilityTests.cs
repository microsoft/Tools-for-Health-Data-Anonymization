// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Anonymizer.Common.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class DicomUtilityTests
    {
        private static readonly TimeSpan _timeSpan = TimeSpan.FromHours(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours);

        public static IEnumerable<object[]> GetValidDicomDateStringForParsing()
        {
            yield return new object[] { "20210613", new DateTimeOffset(2021, 6, 13, 0, 0, 0, _timeSpan) };
            yield return new object[] { "19900101", new DateTimeOffset(1990, 1, 1, 0, 0, 0, _timeSpan) };
            yield return new object[] { "19691120", new DateTimeOffset(1969, 11, 20, 0, 0, 0, _timeSpan) };
            yield return new object[] { "00010102", new DateTimeOffset(1, 1, 2, 0, 0, 0, _timeSpan) };
        }

        public static IEnumerable<object[]> GetValidDicomDateTimeStringForParsing()
        {
            yield return new object[] { "20210613000000+0000", new DateTimeOffset(2021, 6, 13, 0, 0, 0, new TimeSpan(0, 0, 0)), true };
            yield return new object[] { "20210613121212+0800", new DateTimeOffset(2021, 6, 13, 12, 12, 12, new TimeSpan(8, 0, 0)), true };
            yield return new object[] { "19691120010101+1200", new DateTimeOffset(1969, 11, 20, 01, 01, 01, new TimeSpan(12, 0, 0)), true };
            yield return new object[] { "19661111010203.555555+0000", new DateTimeOffset(1966, 11, 11, 1, 2, 3, 555, new TimeSpan(0, 0, 0)), true };
        }

        public static IEnumerable<object[]> GetInvalidDicomDateStringForParsing()
        {
            yield return new object[] { "2021" };
            yield return new object[] { "199001" };
            yield return new object[] { "19691" };
            yield return new object[] { "21" };
            yield return new object[] { "20000000" };
            yield return new object[] { "2000-01-01" };
            yield return new object[] { "200001011212" };
        }

        public static IEnumerable<object[]> GetInvalidDicomDateTimeStringForParsing()
        {
            yield return new object[] { "202111100101" };
            yield return new object[] { "20211111010101." };
            yield return new object[] { "20211111010101.1234567" };
            yield return new object[] { "20211111010101.1234567+500" };
            yield return new object[] { "20211111010101.1234567+08:00" };
            yield return new object[] { "20211111010101.1234567+080000" };
        }

        public static IEnumerable<object[]> GetDateTimeOffsetToGenerateDicomDateString()
        {
            yield return new object[] { new DateTimeOffset(2021, 6, 13, 0, 0, 0, default), "20210613" };
            yield return new object[] { new DateTimeOffset(1990, 1, 1, 0, 0, 0, default), "19900101" };
            yield return new object[] { new DateTimeOffset(1969, 11, 20, 0, 0, 0, default), "19691120" };
            yield return new object[] { new DateTimeOffset(1, 1, 1, 0, 0, 0, default), "00010101" };
        }

        public static IEnumerable<object[]> GetDateTimeOffsetToGenerateDicomDateTimeString()
        {
            yield return new object[]
            {
                new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(2021, 6, 13, 0, 0, 0, new TimeSpan(0, 0, 0)),
                    HasTimeZone = true,
                },
                "20210613000000.000000+0000",
            };
            yield return new object[]
            {
                new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(2021, 6, 13, 12, 12, 12, new TimeSpan(8, 0, 0)),
                    HasTimeZone = false,
                },
                "20210613121212.000000",
            };
            yield return new object[]
            {
                new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(2021, 6, 13, 12, 12, 12, 555, new TimeSpan(8, 0, 0)),
                    HasTimeZone = true,
                },
                "20210613121212.555000+0800",
            };
        }

        public static IEnumerable<object[]> GetValidDicomAgeStringForParsing()
        {
            yield return new object[] { "000Y", 0, AgeType.Year };
            yield return new object[] { "100Y", 100, AgeType.Year };
            yield return new object[] { "010M", 10, AgeType.Month };
            yield return new object[] { "099D", 99, AgeType.Day };
            yield return new object[] { "099W", 99, AgeType.Week };
        }

        public static IEnumerable<object[]> GetInvalidDicomAgeStringForParsing()
        {
            yield return new object[] { "000" };
            yield return new object[] { "1000Y" };
            yield return new object[] { "10Y" };
            yield return new object[] { "Y" };
            yield return new object[] { "010A" };
            yield return new object[] { "099DD" };
            yield return new object[] { "099W010D" };
        }

        public static IEnumerable<object[]> GetAgeValueObjectToGenerateDicomAgeString()
        {
            yield return new object[] { new AgeObject(0, AgeType.Year), "000Y" };
            yield return new object[] { new AgeObject(100, AgeType.Year), "100Y" };
            yield return new object[] { new AgeObject(10, AgeType.Month), "010M" };
            yield return new object[] { new AgeObject(99, AgeType.Day), "099D" };
            yield return new object[] { new AgeObject(99, AgeType.Week), "099W" };
            yield return new object[] { new AgeObject(1000, AgeType.Day), "002Y" };
            yield return new object[] { new AgeObject(2000, AgeType.Month), "166Y" };
        }

        public static IEnumerable<object[]> GetInvalidAgeValueObjectToGenerateDicomAgeString()
        {
            yield return new object[] { new AgeObject(1000, AgeType.Year) };
            yield return new object[] { new AgeObject(12000, AgeType.Month) };
            yield return new object[] { new AgeObject(52000, AgeType.Week) };
            yield return new object[] { new AgeObject(365000, AgeType.Day) };
        }

        [Theory]
        [MemberData(nameof(GetValidDicomDateStringForParsing))]
        public void GivenValidDicomDateStingForParsing_WhenParsing_TheCorrectDateTimeOffsetWillBeReturned(string dateString, DateTimeOffset expectedResult)
        {
            Assert.Equal(expectedResult, DicomUtility.ParseDicomDate(dateString));
        }

        [Theory]
        [MemberData(nameof(GetValidDicomDateStringForParsing))]
        public void GivenValidDicomDAItemForParsing_WhenParsing_TheCorrectDateTimeOffsetWillBeReturned(string dateString, DateTimeOffset expectedResult)
        {
            var item = new DicomDate(DicomTag.PatientBirthDate, dateString, dateString);
            foreach (var output in DicomUtility.ParseDicomDate(item))
            {
                Assert.Equal(expectedResult, output);
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidDicomDateStringForParsing))]
        public void GivenInvalidDicomDateStingForParsing_WhenParsing_ExceptionWillBeThrown(string dateString)
        {
            Assert.Throws<DicomDataException>(() => DicomUtility.ParseDicomDate(dateString));
        }

        [Theory]
        [MemberData(nameof(GetInvalidDicomDateStringForParsing))]
        public void GivenDicomDAItemWithInvalidDateStringForParsing_WhenParsing_ExceptionWillBeThrown(string dateString)
        {
            var item = new DicomDate(DicomTag.PatientBirthDate, dateString);
            Assert.Throws<DicomDataException>(() => DicomUtility.ParseDicomDate(item));
        }

        [Theory]
        [MemberData(nameof(GetValidDicomDateTimeStringForParsing))]
        public void GivenValidDicomDateTimeStingForParsing_WhenParsing_TheCorrectDateTimeOffsetWillBeReturned(string dateTimeString, DateTimeOffset expectedResult, bool hasTimeZone)
        {
            var output = DicomUtility.ParseDicomDateTime(dateTimeString);
            Assert.Equal(expectedResult, output.DateValue);
            Assert.Equal(hasTimeZone, output.HasTimeZone);
        }

        [Theory]
        [MemberData(nameof(GetValidDicomDateTimeStringForParsing))]
        public void GivenValidDicomDTItemForParsing_WhenParsing_TheCorrectDateTimeOffsetWillBeReturned(string dateTimeString, DateTimeOffset expectedResult, bool hasTimeZone)
        {
            var item = new DicomDateTime(DicomTag.AssertionDateTime, dateTimeString, dateTimeString);
            foreach (var output in DicomUtility.ParseDicomDateTime(item))
            {
                Assert.Equal(expectedResult, output.DateValue);
                Assert.Equal(hasTimeZone, output.HasTimeZone);
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidDicomDateTimeStringForParsing))]
        public void GivenInvalidDicomDateTimeStingForParsing_WhenParsing_ExceptionWillBeThrown(string dateTimeString)
        {
            Assert.Throws<DicomDataException>(() => DicomUtility.ParseDicomDateTime(dateTimeString));
        }

        [Theory]
        [MemberData(nameof(GetInvalidDicomDateTimeStringForParsing))]
        public void GivenDicomDTItemWithInvalidDateTimeForParsing_WhenParsing_ExceptionWillBeThrown(string dateTimeString)
        {
            var item = new DicomDateTime(DicomTag.AssertionDateTime, dateTimeString);
            Assert.Throws<DicomDataException>(() => DicomUtility.ParseDicomDateTime(item));
        }

        [Theory]
        [MemberData(nameof(GetDateTimeOffsetToGenerateDicomDateString))]
        public void GivenDateTimeOffset_WhenGenerateDateString_TheCorrectDateStringWillBeReturned(DateTimeOffset input, string expectedDateString)
        {
            Assert.Equal(expectedDateString, DicomUtility.GenerateDicomDateString(input));
        }

        [Theory]
        [MemberData(nameof(GetDateTimeOffsetToGenerateDicomDateTimeString))]
        public void GivenDateTimeOffset_WhenGenerateDateTimeString_TheCorrectDateStringWillBeReturned(DateTimeObject input, string expectedDateString)
        {
            Assert.Equal(expectedDateString, DicomUtility.GenerateDicomDateTimeString(input));
        }

        [Theory]
        [MemberData(nameof(GetValidDicomAgeStringForParsing))]
        public void GivenValidDicomAgeStingForParsing_WhenParsing_TheCorrectAgeValueObjectWillBeReturned(string ageString, uint expectedAgeValue,  AgeType expectedAgeType)
        {
            var output = DicomUtility.ParseAge(ageString);
            Assert.Equal(expectedAgeValue, output.Value);
            Assert.Equal(expectedAgeType, output.AgeType);
        }

        [Theory]
        [MemberData(nameof(GetInvalidDicomAgeStringForParsing))]
        public void GivenInvalidDicomAgeStingForParsing_WhenParsing_ExceptionWillBeThrown(string ageString)
        {
            Assert.Throws<DicomDataException>(() => DicomUtility.ParseAge(ageString));
        }

        [Theory]
        [MemberData(nameof(GetAgeValueObjectToGenerateDicomAgeString))]
        public void GivenAgeValueObject_WhenGenerateDicomAgeString_TheCorrectAgeStringWillBeReturned(AgeObject input, string expectedAgeString)
        {
            Assert.Equal(expectedAgeString, DicomUtility.GenerateAgeString(input));
        }

        [Theory]
        [MemberData(nameof(GetInvalidAgeValueObjectToGenerateDicomAgeString))]
        public void GivenInvalidAgeValueObject_WhenGenerateDicomAgeString_ExceptionWillBeThrown(AgeObject input)
        {
            Assert.Throws<DicomDataException>(() => DicomUtility.GenerateAgeString(input));
        }
    }
}
