// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using EnsureThat;
using Microsoft.Health.DeID.SharedLib.Models;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class DateShiftFunction
    {
        public DateShiftFunction(DateShiftSetting dateShiftSetting = null)
        {
            dateShiftSetting ??= new DateShiftSetting();
            DateShiftKey = dateShiftSetting.DateShiftKey;
            DateShiftKeyPrefix = dateShiftSetting.DateShiftKeyPrefix;
            DateShiftRange = dateShiftSetting.DateShiftRange;
        }

        public string DateShiftKey { get; set; }

        public string DateShiftKeyPrefix { get; set; }

        public uint DateShiftRange { get; set; }

        public string Shift(string inputString, AnonymizerValueTypes valueType)
        {
            return valueType switch
            {
                AnonymizerValueTypes.Date => ShiftDate(inputString),
                AnonymizerValueTypes.DateTime => ShiftDateTime(inputString),
                _ => throw new DeIDFunctionException(DeIDFunctionErrorCode.DateShiftFailed, "Unsupported value type. DateShift only appllable on date or dateTime."),
            };
        }

        public DateTimeOffset Shift(DateTimeOffset dateTime)
        {
            EnsureArg.IsNotNull<DateTimeOffset>(dateTime, nameof(dateTime));

            try
            {
                DateTimeOffset newDateTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
                return newDateTime.AddDays(GetDateShiftValue());
            }
            catch (Exception ex)
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.DateShiftFailed, "Failed to shift date.", ex);
            }
        }

        private string ShiftDate(string inputString)
        {
            EnsureArg.IsNotNull(inputString, nameof(inputString));

            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(inputString, DeIDGlobalSettings.DateFormat);

            return Shift(date).ToString(DeIDGlobalSettings.DateFormat);
        }

        private string ShiftDateTime(string inputString)
        {
            EnsureArg.IsNotNull(inputString, nameof(inputString));

            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(inputString, DeIDGlobalSettings.DateTimeFormat);

            return Shift(date).ToString(DeIDGlobalSettings.DateTimeFormat);
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
