// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class DateTimeUtility
    {
        public static DateTimeOffset ParseDateTime(string dateString, string format = null, IFormatProvider provider = null)
        {
            if (format == null && DateTimeOffset.TryParse(dateString, out DateTimeOffset date))
            {
                return date;
            }
            else
            {
                format ??= Constants.YearFormat;
                if (dateString.Length < format.Length)
                {
                    format = format.Remove(dateString.Length);
                }

                return DateTimeOffset.ParseExact(dateString, format, provider ?? CultureInfo.InvariantCulture);
            }
        }

        public static string DateTimeToString(DateTimeOffset dateTimeOffset, string outputFormat = null)
        {
            outputFormat ??= DeIDGlobalSettings.OutputDateTimeFormat;
            return dateTimeOffset.ToString(outputFormat);
        }

        public static string GetDateTimeFormat(string dateTimeString, string[] formats)
        {
            if (formats?.Any() != true)
            {
                formats = CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns();
            }

            DateTimeFormatInfo formatInfo = DateTimeFormatInfo.CurrentInfo;
            foreach (string pattern in formats)
            {
                if (DateTimeOffset.TryParseExact(dateTimeString, pattern, formatInfo, DateTimeStyles.None, out DateTimeOffset _))
                {
                    return pattern;
                }
            }

            return null;
        }

        public static bool IndicateAgeOverThreshold(DateTimeOffset date)
        {
            int year = date.Year;
            int month = date.Month;
            int day = date.Day;
            int age = DateTimeOffset.Now.Year - year -
                (DateTimeOffset.Now.Month < month || (DateTimeOffset.Now.Month == month && DateTimeOffset.Now.Day < day) ? 1 : 0);

            return age > Constants.AgeThreshold;
        }
    }
}
