// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.DeID.SharedLib.Models;
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

        public string Redact(string inputString, AnonymizerValueTypes valueType)
        {
            return valueType switch
            {
                AnonymizerValueTypes.Date => RedactDate(inputString),
                AnonymizerValueTypes.DateTime => RedactDateTime(inputString),
                AnonymizerValueTypes.Age => RedactAge(decimal.Parse(inputString)).ToString(),
                AnonymizerValueTypes.PostalCode => RedactPostalCode(inputString),
                _ => null,
            };
        }

        public uint? Redact(uint value, AnonymizerValueTypes valueType)
        {
            return valueType switch
            {
                AnonymizerValueTypes.Age => RedactAge(value),
                _ => null,
            };
        }

        public decimal? Redact(decimal value, AnonymizerValueTypes valueType)
        {
            return valueType switch
            {
                AnonymizerValueTypes.Age => RedactAge(value),
                _ => null,
            };
        }

        public DateTimeOffset? Radact(DateTimeOffset dateTime)
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

        public DateTimeObject Redact(DateTimeObject dateObject)
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

        public AgeObject Redact(AgeObject age)
        {
            EnsureArg.IsNotNull(age, nameof(age));

            return RedactAge(age.AgeInYears()) == null ? null : age;
        }

        private string RedactDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }

            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(dateString, DeIDGlobalSettings.DateFormat);

            return Radact(date)?.ToString(DeIDGlobalSettings.DateFormat);
        }

        private string RedactDateTime(string dateTimeString)
        {
            if (string.IsNullOrEmpty(dateTimeString))
            {
                return null;
            }

            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(dateTimeString, DeIDGlobalSettings.DateTimeFormat);

            return Radact(date)?.ToString(DeIDGlobalSettings.DateTimeFormat);
        } 

        private uint? RedactAge(uint age)
        {
            return (uint?)RedactAge((decimal)age);
        }

        private decimal? RedactAge(decimal age)
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

        private string RedactPostalCode(string postalCode)
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
