// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class CryptoHashFunction
    {
        public static byte[] ComputeHmacSHA256Hash(byte[] input, byte[] hashKey = null)
        {
            if (input == null)
            {
                return input;
            }

            HMAC hmac = new HMACSHA256();
            if (hashKey != null)
            {
                hmac = new HMACSHA256(hashKey);
            }

            return hmac.ComputeHash(input);
        }

        public static byte[] ComputeHmacSHA256Hash(Stream input, byte[] hashKey = null)
        {
            if (input == null)
            {
                return null;
            }

            HMAC hmac = new HMACSHA256();
            if (hashKey != null)
            {
                hmac = new HMACSHA256(hashKey);
            }

            return hmac.ComputeHash(input);
        }

        public static byte[] ComputeHmacSHA256Hash(string input, byte[] hashKey = null, Encoding encoding = null)
        {
            if (input == null)
            {
                return null;
            }

            encoding ??= Encoding.UTF8;
            return ComputeHmacSHA256Hash(encoding.GetBytes(input), hashKey);
        }

        public static FixedLengthString ComputeHmacSHA256Hash(FixedLengthString input, byte[] hashKey = null)
        {
            if (input == null)
            {
                return input;
            }

            var hashData = ComputeHmacSHA256Hash(input.ToString(), hashKey);

            return new FixedLengthString(input.GetLength(), string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        public static byte[] ComputeHmacHash(byte[] input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            if (input == null)
            {
                return null;
            }

            return hashAlgorithm.ComputeHash(input);
        }

        public static byte[] ComputeHmacHash(string input, HMAC hashAlgorithm, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            if (input == null)
            {
                return null;
            }

            encoding ??= Encoding.UTF8;
            return hashAlgorithm.ComputeHash(encoding.GetBytes(input));
        }

        public static byte[] ComputeHmacHash(Stream input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            if (input == null)
            {
                return null;
            }

            return hashAlgorithm.ComputeHash(input);
        }

        public static FixedLengthString ComputeHmacHash(FixedLengthString input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            if (input == null)
            {
                return input;
            }

            var hashData = ComputeHmacHash(input.ToString(), hashAlgorithm);

            input.SetString(string.Concat(hashData.Select(b => b.ToString("x2"))));
            return input;
        }
    }
}
