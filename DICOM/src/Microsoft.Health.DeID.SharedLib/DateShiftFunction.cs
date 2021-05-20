// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class DateShiftFunction
    {
        public DateShiftFunction(DateShiftSetting dateShiftSetting)
        {
            if (dateShiftSetting != null)
            {
                DateShiftKey = dateShiftSetting.DateShiftKey;
                DateShiftKeyPrefix = dateShiftSetting.DateShiftKeyPrefix;
                DateShiftRange = dateShiftSetting.DateShiftRange;
            }
        }

        public string DateShiftKey { get; set; } = string.Empty;

        public string DateShiftKeyPrefix { get; set; } = string.Empty;

        public uint DateShiftRange { get; set; } = 50;

        public string ShiftDate(string inputString, string inputDateTimeFormat = null, string outputDateTimeFormat = null, IFormatProvider provider = null)
        {
            if (inputString == null)
            {
                return null;
            }

            var date = DateTimeUtility.ParseDateTime(inputString, inputDateTimeFormat, provider);
            var outputFormat = outputDateTimeFormat ?? DeIDGlobalSettings.OutputDateTimeFormat ?? inputDateTimeFormat ?? DateTimeUtility.GetDateTimeFormat(inputString, null);
            if (string.IsNullOrEmpty(outputFormat))
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.InvalidDeIdSettings, "Output date format not specified.");
            }

            return date.AddDays(GetDateShiftValue()).ToString(outputFormat);
        }

        public string ShiftDateTime(string inputString, string inputDateTimeFormat = null, string outputDateTimeFormat = null, IFormatProvider provider = null)
        {
            if (inputString == null)
            {
                return null;
            }

            var date = DateTimeUtility.ParseDateTime(inputString, inputDateTimeFormat, provider);
            var outputFormat = outputDateTimeFormat ?? DeIDGlobalSettings.OutputDateTimeFormat ?? inputDateTimeFormat ?? DateTimeUtility.GetDateTimeFormat(inputString, null);
            if (string.IsNullOrEmpty(outputFormat))
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.InvalidDeIdSettings, "Output date format not specified.");
            }

            DateTimeOffset newDateTime = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
            return newDateTime.AddDays(GetDateShiftValue()).ToString(outputFormat);
        }

        public DateTimeOffset ShiftDateTime(DateTimeOffset dateTime)
        {
            EnsureArg.IsNotNull<DateTimeOffset>(dateTime, nameof(dateTime));

            DateTimeOffset newDateTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
            return newDateTime.AddDays(GetDateShiftValue());
        }

        private int GetDateShiftValue()
        {
            int offset = 0;
            var bytes = Encoding.UTF8.GetBytes(DateShiftKeyPrefix + DateShiftKey);
            foreach (byte b in bytes)
            {
                offset = (int)(((offset * Constants.DateShiftSeed) + (int)b) % ((2 * DateShiftRange) + 1));
            }

            offset -= (int)DateShiftRange;

            return offset;
        }
    }
}
