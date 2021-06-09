// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.DeID.SharedLib.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    /// <summary>
    /// Utility functions are used to parse DICOM data including DA, DT, AS. The format for these VR is given in DICOM standard.
    /// http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part05/sect_6.2.html
    /// </summary>
    public static class Utility
    {
        public static readonly Dictionary<string, AgeType> AgeTypeMapping = new Dictionary<string, AgeType>
            {
                { "Y", AgeType.Year },
                { "M", AgeType.Month },
                { "W", AgeType.Week },
                { "D", AgeType.Day },
            };

        public static DateTimeOffset[] ParseDicomDate(DicomDate item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return item.Get<string[]>().Select(ParseDicomDate).ToArray();
        }

        public static DateTimeOffset ParseDicomDate(string date)
        {
            EnsureArg.IsNotNull(date, nameof(date));

            try
            {
                return DateTimeOffset.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new DicomDataException("Invalid date value. The valid format is YYYYMMDD.");
            }
        }

        public static DateTimeObject[] ParseDicomDateTime(DicomDateTime item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return item.Get<string[]>().Select(ParseDicomDateTime).ToArray();
        }

        public static DateTimeObject ParseDicomDateTime(string dateTime)
        {
            EnsureArg.IsNotNull(dateTime, nameof(dateTime));

            Regex dateTimeRegex = new Regex(@"^((?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<hour>\d{2})(?<minute>\d{2})(?<second>\d{2})(\.(?<millisecond>\d{1,6}))?(?<timeZone>(?<sign>-|\+)(?<timeZoneHour>\d{2})(?<timeZoneMinute>\d{2}))?)(\s*)");
            var matches = dateTimeRegex.Matches(dateTime);
            if (matches.Count != 1)
            {
                throw new DicomDataException("Invalid date time value. The valid format is YYYYMMDDHHMMSS.FFFFFF&ZZXX.");
            }

            var groups = matches[0].Groups;

            int year = groups["year"].Success ? int.Parse(groups["year"].Value) : 0;
            int month = groups["month"].Success ? int.Parse(groups["month"].Value) : 0;
            int day = groups["day"].Success ? int.Parse(groups["day"].Value) : 0;
            int hour = groups["hour"].Success ? int.Parse(groups["hour"].Value) : 0;
            int minute = groups["minute"].Success ? int.Parse(groups["minute"].Value) : 0;
            int second = groups["second"].Success ? int.Parse(groups["second"].Value) : 0;
            int millisecond = groups["millisecond"].Success ? int.Parse(groups["millisecond"].Value) : 0;
            millisecond = millisecond > 999 ? int.Parse(millisecond.ToString().Substring(0, 3)) : millisecond;

            if (groups["timeZone"].Success)
            {
                int timeZoneHour = int.Parse(groups["timeZoneHour"].Value) * int.Parse(groups["sign"].Value + "1");
                int timeZoneMinute = int.Parse(groups["timeZoneMinute"].Value);
                return new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, new TimeSpan(timeZoneHour, timeZoneMinute, 0)),
                    HasTimeZone = true,
                };
            }
            else
            {
                return new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, default),
                    HasTimeZone = false,
                };
            }
        }

        public static string GenerateDicomDateString(DateTimeOffset date)
        {
            if (date == null)
            {
                return null;
            }

            return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public static string GenerateDicomDateTimeString(DateTimeObject date)
        {
            if (date == null)
            {
                return null;
            }

            if (!(bool)date.HasTimeZone)
            {
                return date.DateValue.ToString("yyyyMMddhhmmss.ffffff", CultureInfo.InvariantCulture);
            }
            else
            {
                return date.DateValue.ToString("yyyyMMddHHmmss.ffffffzzz", CultureInfo.InvariantCulture).Replace(":", string.Empty);
            }
        }

        public static AgeValue ParseAge(string age)
        {
            if (string.IsNullOrEmpty(age))
            {
                return null;
            }

            foreach (var item in AgeTypeMapping)
            {
                if (new Regex(@"\d{3}" + item.Key).IsMatch(age))
                {
                    return new AgeValue(uint.Parse(age.Substring(0, Constants.AgeStringLength)), item.Value);
                }
            }

            if (uint.TryParse(age, out uint result))
            {
                return new AgeValue(result, AgeType.Year);
            }
            else
            {
                throw new DicomDataException("Invalid age string. The valid strings are nnnD, nnnW, nnnM, nnnY.");
            }
        }

        public static string AgeToString(AgeValue age)
        {
            if (age == null)
            {
                return null;
            }

            foreach (var item in AgeTypeMapping)
            {
                if (age.AgeType == item.Value)
                {
                    // Age string only support 3 charaters
                    if (age.Age <= 999)
                    {
                        return age.Age.ToString().PadLeft(Constants.AgeStringLength, '0') + item.Key;
                    }
                    else if (age.AgeToYearsOld() <= 999)
                    {
                        return age.AgeToYearsOld().ToString().PadLeft(Constants.AgeStringLength, '0') + item.Key;
                    }

                    throw new DicomDataException("Invalid age value for DICOM. The valid strings are nnnD, nnnW, nnnM, nnnY.");
                }
            }

            return null;
        }
    }
}
