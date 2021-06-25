// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Anonymizer.Common
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
            SourceValue = sourceValue;
            Value = sourceValue;
        }

        public FixedLengthString(int length, string sourceValue)
        {
            EnsureArg.IsNotNull(sourceValue, nameof(sourceValue));

            Length = length;
            SetString(sourceValue);
        }

        public string Value { get; private set; }

        public int Length { get; private set; }

        public string SourceValue { get; private set; }

        public override string ToString()
        {
            return Value;
        }

        public void SetString(string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));

            if (value.Length >= Length)
            {
                Value = value.Substring(0, Length);
            }
            else
            {
                Value = value.PadRight(Length, '0');
            }

            SourceValue = value;
        }
    }
}
