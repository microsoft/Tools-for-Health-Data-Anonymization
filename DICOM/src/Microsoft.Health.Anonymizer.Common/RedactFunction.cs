// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Models;
using Microsoft.Health.Anonymizer.Common.Settings;

namespace Microsoft.Health.Anonymizer.Common
{
    public class RedactFunction
    {
        private const string ReplacementDigit = "0";
        private const int InitialDigitsCount = 3;
        private readonly RedactSetting _redactSetting;

        public RedactFunction(RedactSetting redactSetting)
        {
            EnsureArg.IsNotNull(redactSetting, nameof(redactSetting));

            _redactSetting = redactSetting ?? new RedactSetting();
        }

        public string Redact(string inputString, AnonymizerValueTypes valueType)
        {
            if (inputString == null)
            {
                return null;
            }

            switch (valueType)
            {
                case AnonymizerValueTypes.Date:
                    return RedactDateTime(inputString, DateTimeGlobalSettings.DateFormat);
                case AnonymizerValueTypes.DateTime:
                    return RedactDateTime(inputString, DateTimeGlobalSettings.DateTimeFormat);
                case AnonymizerValueTypes.Age:
                    try
                    {
                        return RedactAge(decimal.Parse(inputString)).ToString();
                    }
                    catch
                    {
                        throw new AnonymizerException(AnonymizerErrorCode.RedactFailed, "The input value is not a numeric age value.");
                    }

                case AnonymizerValueTypes.PostalCode:
                    return RedactPostalCode(inputString);
                default:
                    return null;
            }
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

            if (_redactSetting.EnablePartialDatesForRedact && !DateTimeUtility.IndicateAgeOverThreshold(dateTime))
            {
                return new DateTimeOffset(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Offset);
            }

            return null;
        }

        public DateTimeObject Redact(DateTimeObject dateObject)
        {
            EnsureArg.IsNotNull(dateObject, nameof(dateObject));

            if (_redactSetting.EnablePartialDatesForRedact && !DateTimeUtility.IndicateAgeOverThreshold(dateObject.DateValue))
            {
                dateObject.DateValue = new DateTimeOffset(dateObject.DateValue.Year, 1, 1, 0, 0, 0, (bool)dateObject.HasTimeZone ? dateObject.DateValue.Offset : default);
                return dateObject;
            }

            return null;
        }

        public AgeObject Redact(AgeObject age)
        {
            EnsureArg.IsNotNull(age, nameof(age));

            return RedactAge(age.AgeInYears()) == null ? null : age;
        }

        private string RedactDateTime(string dateTimeString, string dateTimeFormat)
        {
            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(dateTimeString, dateTimeFormat);
            return Radact(date)?.ToString(dateTimeFormat);
        }

        private uint? RedactAge(uint age)
        {
            return (uint?)RedactAge((decimal)age);
        }

        private decimal? RedactAge(decimal age)
        {
            if (_redactSetting.EnablePartialAgesForRedact)
            {
                if (age > Constants.AgeThreshold)
                {
                    return null;
                }

                return age;
            }

            return null;
        }

        private string RedactPostalCode(string postalCode)
        {
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
