// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class DateTimeUtility
    {
        public static DateTimeOffset ParseDateTimeString(string inputString, string inputDateTimeFormat, IFormatProvider provider, string globalFormat = null)
        {
            if (inputDateTimeFormat != null)
            {
                if (DateTimeOffset.TryParseExact(inputString, inputDateTimeFormat, provider ?? CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset result))
                {
                    return result;
                }
            }

            if (DateTimeOffset.TryParse(inputString, out DateTimeOffset defaultResult))
            {
                return defaultResult;
            }

            if (DateTimeOffset.TryParseExact(inputString, globalFormat, provider ?? CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset globalResult))
            {
                return globalResult;
            }

            if (DateTimeOffset.TryParseExact(inputString, "yyyy", provider ?? CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset yearResult))
            {
                return yearResult;
            }

            throw new DeIDFunctionException(DeIDFunctionErrorCode.InvalidInputValue, "Failed to parse input date value.");
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
