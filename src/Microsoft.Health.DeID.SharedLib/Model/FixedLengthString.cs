// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class FixedLengthString
    {
        private string value;
        private readonly int length;
        private string sourceValue;

        public FixedLengthString(int length)
            : this(length, string.Empty)
        {
        }

        public FixedLengthString(string sourceValue)
        {
            EnsureArg.IsNotNull(sourceValue, nameof(sourceValue));

            length = sourceValue.Length;
            this.sourceValue = sourceValue;
            value = sourceValue;
        }

        public FixedLengthString(int length, string sourceValue)
        {
            this.length = length;
            this.sourceValue = sourceValue;
            if (sourceValue.Length > length)
            {
                value = sourceValue.Substring(0, length);
            }
            else
            {
                value = sourceValue + new string('0', length - sourceValue.Length);
            }

        }

        public override string ToString()
        {
            return value;
        }

        public void SetString(string newstring)
        {
            if (newstring.Length > length)
            {
                value = newstring.Substring(0, length);
            }
            else
            {
                value = newstring + new string('0', length - newstring.Length);
            }

            sourceValue = newstring;
        }

        public int GetLength()
        {
            return length;
        }
    }
}
