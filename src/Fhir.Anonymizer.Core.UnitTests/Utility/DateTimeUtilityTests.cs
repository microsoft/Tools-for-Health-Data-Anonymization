using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class DateTimeUtilityTests
    {
        public static IEnumerable<object[]> GetDateDataForPartialRedact()
        {
            yield return new object[] { new Date("2015"), new Date("2015") };
            yield return new object[] { new Date("2015-02"), new Date("2015") };
            yield return new object[] { new Date("2015-02-07"), new Date("2015") };
            yield return new object[] { new Date("1925-02-07"), null };
        }

        public static IEnumerable<object[]> GetDateDataForRedact()
        {
            yield return new object[] { new Date("2015") };
            yield return new object[] { new Date("2015-02") };
            yield return new object[] { new Date("2015-02-07") };
            yield return new object[] { new Date("1925-02-07") };
        }

        public static IEnumerable<object[]> GetDateDataForDateShift()
        {
            yield return new object[] { new Date("2015-02-07"), new Date("2014-12-19"), new Date("2015-03-29") };
            yield return new object[] { new Date("2020-01-17"), new Date("2019-11-28"), new Date("2020-03-07") };
            yield return new object[] { new Date("1998-10-02"), new Date("1998-08-13"), new Date("1998-11-21") };
            yield return new object[] { new Date("1975-12-26"), new Date("1975-11-06"), new Date("1976-02-14") };
        }

        public static IEnumerable<object[]> GetDateDataForDateShiftButShouldBeRedacted()
        {
            yield return new object[] { new Date("2015-02"), new Date("2015") };
            yield return new object[] { new Date("1925-02-07"), null };
        }

        public static IEnumerable<object[]> GetDateTimeDataForRedact()
        {
            yield return new object[] { new FhirDateTime("2015"), new FhirDateTime("2015") };
            yield return new object[] { new FhirDateTime("2015-02"), new FhirDateTime("2015") };
            yield return new object[] { new FhirDateTime("2015-02-07"), new FhirDateTime("2015") };
            yield return new object[] { new FhirDateTime("2015-02-07T13:28:17-05:00"), new FhirDateTime("2015") };
            yield return new object[] { new FhirDateTime("1925-02-07T13:28:17-05:00"), null };
        }

        public static IEnumerable<object[]> GetDateTimeDataForDateShift()
        {
            yield return new object[] { new FhirDateTime("2015-02-07"), new FhirDateTime("2014-12-19"), new FhirDateTime("2015-03-29") };
            yield return new object[] { new FhirDateTime("2015-02-07T13:28:17-05:00"), new FhirDateTime("2014-12-19T00:00:00-05:00"), new FhirDateTime("2015-03-29T00:00:00-05:00") };
            yield return new object[] { new FhirDateTime("1998-10-02"), new FhirDateTime("1998-08-13"), new FhirDateTime("1998-11-21") };
            yield return new object[] { new FhirDateTime("1998-10-02T08:47:25+08:00"), new FhirDateTime("1998-08-13T00:00:00+08:00"), new FhirDateTime("1998-11-21T00:00:00+08:00") };
        }

        public static IEnumerable<object[]> GetDateTimeDataForDateShiftFormatTest()
        {
            yield return new object[] { "dummy", new FhirDateTime("2015-02-07"), "2015-01-17" };
            yield return new object[] { "dummy", new FhirDateTime("2015-02-07T13:28:17-05:00"), "2015-01-17T00:00:00-05:00" };
            yield return new object[] { "dummy", new FhirDateTime("2015-02-07T13:28:17+05:00"), "2015-01-17T00:00:00+05:00" };
            yield return new object[] { "dummy", new FhirDateTime("2015-02-07T13:28:17Z"), "2015-01-17T00:00:00Z" };
            yield return new object[] { "dummy", new FhirDateTime("2015-02-07T13:28:17.12345-05:00"), "2015-01-17T00:00:00.00000-05:00" };
        }

        public static IEnumerable<object[]> GetDateTimeDataForDateShiftButShouldBeRedacted()
        {
            yield return new object[] { new FhirDateTime("2015-02"), new FhirDateTime("2015") };
            yield return new object[] { new FhirDateTime("1925-02-07T13:28:17-05:00"), null };
        }

        public static IEnumerable<object[]> GetAgeDataForPartialRedact()
        {
            yield return new object[] { new Age { Value = 92 } };
            yield return new object[] { new Age { Value = 57 } };
        }

        public static IEnumerable<object[]> GetAgeDataForRedact()
        {
            yield return new object[] { new Age { Value = 101 } };
            yield return new object[] { new Age { Value = 35 } };
        }

        [Theory]
        [MemberData(nameof(GetDateDataForPartialRedact))]
        public void GivenADate_WhenPartialRedact_ThenDateShouldBeRedacted(Date date, Date expectedDate)
        {
            var node = ElementNode.FromElement(date.ToTypedElement());
            DateTimeUtility.RedactDateNode(node, true);

            Assert.Equal(expectedDate?.ToString() ?? null, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetDateDataForRedact))]
        public void GivenADate_WhenRedact_ThenDateShouldBeRedacted(Date date)
        {
            var node = ElementNode.FromElement(date.ToTypedElement());
            DateTimeUtility.RedactDateNode(node, false);

            Assert.Null(node.Value);
        }

        [Theory]
        [MemberData(nameof(GetDateDataForDateShift))]
        public void GivenADate_WhenDateShift_ThenDateShouldBeShifted(Date dateTime, DateTime minExpectedDate, DateTime maxExpectedDate)
        {
            var node = ElementNode.FromElement(dateTime.ToTypedElement());
            DateTimeUtility.ShiftDateNode(node, string.Empty, true);

            Assert.True(minExpectedDate <= DateTime.Parse(node.Value.ToString()));
            Assert.True(maxExpectedDate >= DateTime.Parse(node.Value.ToString()));
        }

        [Theory]
        [MemberData(nameof(GetDateDataForDateShiftButShouldBeRedacted))]
        public void GivenADateWithoutDayOrAgeOver89_WhenDateShift_ThenDateShouldBeRedacted(Date date, Date expectedDate)
        {
            var node = ElementNode.FromElement(date.ToTypedElement());
            DateTimeUtility.ShiftDateNode(node, string.Empty, true);

            Assert.Equal(expectedDate?.ToString() ?? null, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeDataForRedact))]
        public void GivenADateTime_WhenRedact_ThenDateTimeShouldBeRedacted(FhirDateTime dateTime, FhirDateTime expectedDateTime)
        {
            var node = ElementNode.FromElement(dateTime.ToTypedElement());
            DateTimeUtility.RedactDateTimeAndInstantNode(node, true);

            Assert.Equal(expectedDateTime?.ToString() ?? null, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetDateTimeDataForDateShift))]
        public void GivenADateTime_WhenDateShift_ThenDateTimeShouldBeShifted(FhirDateTime dateTime, FhirDateTime minExpectedDateTime, FhirDateTime maxExpectedDateTime)
        {
            var node = ElementNode.FromElement(dateTime.ToTypedElement());
            DateTimeUtility.ShiftDateTimeAndInstantNode(node, Guid.NewGuid().ToString("N"), true);

            Assert.True(minExpectedDateTime <= new FhirDateTime(node.Value.ToString()));
            Assert.True(maxExpectedDateTime >= new FhirDateTime(node.Value.ToString()));
        }

        [Theory]
        [MemberData(nameof(GetDateTimeDataForDateShiftFormatTest))]
        public void GivenADateTime_WhenDateShift_ThenDateTimeFormatShouldNotChange(string dateShiftKey, FhirDateTime dateTime, string expectedDateTimeString)
        {
            var node = ElementNode.FromElement(dateTime.ToTypedElement());
            DateTimeUtility.ShiftDateTimeAndInstantNode(node, dateShiftKey, true);
            Assert.Equal(expectedDateTimeString, node.Value.ToString());
        }

        [Theory]
        [MemberData(nameof(GetDateTimeDataForDateShiftButShouldBeRedacted))]
        public void GivenADateTimeWithoutDayOrAgeOver89_WhenDateShift_ThenDateTimeShouldBeRedacted(FhirDateTime dateTime, FhirDateTime expectedDateTime)
        {
            var node = ElementNode.FromElement(dateTime.ToTypedElement());
            DateTimeUtility.ShiftDateTimeAndInstantNode(node, string.Empty, true);

            Assert.Equal(expectedDateTime?.ToString() ?? null, node.Value);
        }

        [Theory]
        [MemberData(nameof(GetAgeDataForPartialRedact))]
        public void GivenAnAge_WhenPartialRedact_ThenAgeOver89ShouldBeRedacted(Age age)
        {
            var node = ElementNode.FromElement(age.ToTypedElement()).Children("value").Cast<ElementNode>().FirstOrDefault();
            DateTimeUtility.RedactAgeDecimalNode(node, true);

            Assert.Equal(int.Parse(age.Value.ToString()) > 89 ? null : age.Value.ToString(), node.Value?.ToString() ?? null);
        }

        [Theory]
        [MemberData(nameof(GetAgeDataForRedact))]
        public void GivenAnAge_WhenRedact_ThenAgeShouldBeRedacted(Age age)
        {
            var node = ElementNode.FromElement(age.ToTypedElement()).Children("value").Cast<ElementNode>().FirstOrDefault();
            DateTimeUtility.RedactAgeDecimalNode(node, false);

            Assert.Null(node.Value);
        }
    }
}
