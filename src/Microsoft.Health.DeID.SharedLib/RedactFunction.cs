// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Dicom.DeID.SharedLib.Model;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class RedactFunction
    {
        private static readonly int AgeThreshold = 89;
        private static readonly string ReplacementDigit = "0";
        private static readonly int InitialDigitsCount = 3;

        public RedactFunction(RedactSetting redactSetting)
        {
            Settings = redactSetting;
        }

        public RedactSetting Settings { get; set; }

        public string RedactDateTime(string dateTime, string dateTimeFormat = null, IFormatProvider provider = null)
        {
            if (string.IsNullOrEmpty(dateTime))
            {
                return null;
            }

            if (Settings.EnablePartialDatesForRedact)
            {
                DateTimeOffset date = DateTimeUtility.ParseDateTime(dateTime, dateTimeFormat, provider);
                return DateTimeUtility.IndicateAgeOverThreshold(date) ? null : date.Year.ToString();
            }
            else
            {
                return null;
            }
        }

        public DateTimeOffset? RedactDateTime(DateTimeOffset dateTime)
        {
            EnsureArg.IsNotNull<DateTimeOffset>(dateTime, nameof(dateTime));

            if (Settings.EnablePartialDatesForRedact)
            {
                if (DateTimeUtility.IndicateAgeOverThreshold(dateTime))
                {
                    return null;
                }

                return new DateTimeOffset(dateTime.Year, 1, 1, 0, 0, 0, default);
            }
            else
            {
                return null;
            }
        }

        public DateTimeObject RedactDateTime(DateTimeObject dateObject)
        {
            EnsureArg.IsNotNull(dateObject, nameof(dateObject));

            if (Settings.EnablePartialDatesForRedact)
            {
                if (DateTimeUtility.IndicateAgeOverThreshold(dateObject.DateValue))
                {
                    return null;
                }

                dateObject.DateValue = new DateTimeOffset(dateObject.DateValue.Year, 1, 1, 0, 0, 0, dateObject.HasTimeZone == null || !(bool)dateObject.HasTimeZone ? new TimeSpan(0, 0, 0) : dateObject.DateValue.Offset);
                return dateObject;
            }
            else
            {
                return null;
            }
        }

        public int? RedactAge(int age)
        {
            if (Settings.EnablePartialAgeForRedact)
            {
                if (age > AgeThreshold)
                {
                    return null;
                }

                return age;
            }
            else
            {
                return null;
            }
        }

        public decimal? RedactAge(decimal? age)
        {
            if (age == null)
            {
                return null;
            }

            if (Settings.EnablePartialAgeForRedact)
            {
                if (age > AgeThreshold)
                {
                    return null;
                }

                return age;
            }
            else
            {
                return null;
            }
        }

        public AgeValue RedactAge(AgeValue age)
        {
            if (Settings.EnablePartialAgeForRedact)
            {
                if (age.AgeToYearsOld() > AgeThreshold)
                {
                    return null;
                }

                return age;
            }
            else
            {
                return null;
            }
        }

        public string RedactPostalCode(string postalCode)
        {
            if (Settings.EnablePartialZipCodesForRedact)
            {
                if (Settings.RestrictedZipCodeTabulationAreas != null && Settings.RestrictedZipCodeTabulationAreas.Any(x => postalCode.StartsWith(x)))
                {
                    postalCode = Regex.Replace(postalCode, @"\d", ReplacementDigit);
                }
                else if (postalCode.Length >= InitialDigitsCount)
                {
                    var suffix = postalCode[InitialDigitsCount..];
                    postalCode = $"{postalCode.Substring(0, InitialDigitsCount)}{Regex.Replace(suffix, @"\d", ReplacementDigit)}";
                }

                return postalCode;
            }
            else
            {
                return null;
            }
        }
    }
}
