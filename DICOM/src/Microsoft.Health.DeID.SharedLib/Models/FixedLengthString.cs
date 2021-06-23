// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class FixedLengthString
    {
        public FixedLengthString(int length)
            : this(length, string.Empty)
        {
        }

        public FixedLengthString(string sourceValue)
        {
            EnsureArg.IsNotNull(sourceValue, nameof(sourceValue));

            Length = sourceValue.Length;
            this.SourceValue = sourceValue;
            Value = sourceValue;
        }

        public FixedLengthString(int length, string sourceValue)
        {
            this.Length = length;
            this.SourceValue = sourceValue;
            if (sourceValue.Length > length)
            {
                Value = sourceValue.Substring(0, length);
            }
            else
            {
                Value = sourceValue + new string('0', length - sourceValue.Length);
            }
        }

        public string Value { get; private set; }

        public int Length { get; private set; }

        public string SourceValue { get; private set; }

        public override string ToString()
        {
            return Value;
        }

        public void SetString(string newstring)
        {
            if (newstring.Length > Length)
            {
                Value = newstring.Substring(0, Length);
            }
            else
            {
                Value = newstring + new string('0', Length - newstring.Length);
            }

            SourceValue = newstring;
        }

        public int GetLength()
        {
            return Length;
        }
    }
}
