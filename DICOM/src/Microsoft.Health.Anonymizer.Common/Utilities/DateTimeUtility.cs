// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Anonymizer.Common.Exceptions;

namespace Microsoft.Health.Anonymizer.Common
{
    public class DateTimeUtility
    {
        public static DateTimeOffset ParseDateTimeString(string inputString, string globalFormat = null)
        {
            if (DateTimeOffset.TryParse(inputString, out DateTimeOffset defaultResult))
            {
                return defaultResult;
            }

            if (DateTimeOffset.TryParseExact(inputString, globalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset globalResult))
            {
                return globalResult;
            }

            if (DateTimeOffset.TryParseExact(inputString, Constants.YearFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset yearResult))
            {
                return yearResult;
            }

            throw new AnonymizerException(AnonymizerErrorCode.InvalidInputValue, $"Failed to parse input date value: [{inputString}].");
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
