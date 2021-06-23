// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Dicom.DeID.SharedLib.Models;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class RedactFunction
    {
        private static readonly int AgeThreshold = 89;
        private static readonly string ReplacementDigit = "0";
        private static readonly int InitialDigitsCount = 3;
        private readonly RedactSetting _redactSetting;

        public RedactFunction(RedactSetting redactSetting = null)
        {
            _redactSetting = redactSetting ?? new RedactSetting();
        }

        public string RedactDate(string dateString, string inputDateFormat = null, string outputDateFormat = null, IFormatProvider provider = null)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }

            if (_redactSetting.EnablePartialDatesForRedact)
            {
                DateTimeOffset date = DateTimeUtility.ParseDateTimeString(dateString, inputDateFormat, provider, DeIDGlobalSettings.DateFormat);
                return DateTimeUtility.IndicateAgeOverThreshold(date)
                    ? null
                    : new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)).ToString(outputDateFormat ?? DeIDGlobalSettings.DateFormat);
            }

            return null;
        }

        public string RedactDateTime(string dateTimeString, string inputDateTimeFormat = null, string outputDateFormat = null, IFormatProvider provider = null)
        {
            if (string.IsNullOrEmpty(dateTimeString))
            {
                return null;
            }

            if (_redactSetting.EnablePartialDatesForRedact)
            {
                DateTimeOffset date = DateTimeUtility.ParseDateTimeString(dateTimeString, inputDateTimeFormat, provider);
                return DateTimeUtility.IndicateAgeOverThreshold(date)
                    ? null
                    : new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)).ToString(outputDateFormat ?? DeIDGlobalSettings.DateTimeFormat);
            }

            return null;
        }

        public DateTimeOffset? RedactDateTime(DateTimeOffset dateTime)
        {
            EnsureArg.IsNotNull<DateTimeOffset>(dateTime, nameof(dateTime));

            if (_redactSetting.EnablePartialDatesForRedact)
            {
                if (DateTimeUtility.IndicateAgeOverThreshold(dateTime))
                {
                    return null;
                }

                return new DateTimeOffset(dateTime.Year, 1, 1, 0, 0, 0, default);
            }

            return null;
        }

        public DateTimeObject RedactDateTime(DateTimeObject dateObject)
        {
            EnsureArg.IsNotNull(dateObject, nameof(dateObject));

            if (_redactSetting.EnablePartialDatesForRedact)
            {
                if (DateTimeUtility.IndicateAgeOverThreshold(dateObject.DateValue))
                {
                    return null;
                }

                dateObject.DateValue = new DateTimeOffset(dateObject.DateValue.Year, 1, 1, 0, 0, 0, dateObject.HasTimeZone == true ? dateObject.DateValue.Offset : new TimeSpan(0, 0, 0));
                return dateObject;
            }

            return null;
        }

        public int? RedactAge(int age)
        {
            return (int?)RedactAge((decimal)age);
        }

        public decimal? RedactAge(decimal age)
        {
            if (_redactSetting.EnablePartialAgesForRedact)
            {
                if (age > AgeThreshold)
                {
                    return null;
                }

                return age;
            }

            return null;
        }

        public AgeObject RedactAge(AgeObject age)
        {
            EnsureArg.IsNotNull(age, nameof(age));

            return RedactAge(age.AgeInYears()) == null ? null : age;
        }

        public string RedactPostalCode(string postalCode)
        {
            EnsureArg.IsNotNull(postalCode, nameof(postalCode));

            if (_redactSetting.EnablePartialZipCodesForRedact)
            {
                if (_redactSetting.RestrictedZipCodeTabulationAreas != null && _redactSetting.RestrictedZipCodeTabulationAreas.Any(x => postalCode.StartsWith(x)))
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

            return null;
        }
    }
}
