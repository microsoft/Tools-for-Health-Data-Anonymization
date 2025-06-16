// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Settings;

namespace Microsoft.Health.Anonymizer.Common
{
    public class CryptoHashFunction
    {
        private readonly HMAC _hmac;
        private readonly CryptoHashSetting _cryptoHashSetting;

        public CryptoHashFunction(CryptoHashSetting cryptoHashSetting)
        {
            EnsureArg.IsNotNull(cryptoHashSetting, nameof(cryptoHashSetting));

            byte[] byteKey = cryptoHashSetting.GetCryptoHashByteKey();
            _hmac = cryptoHashSetting.CryptoHashType switch
            {
                HashAlgorithmType.Sha256 => new HMACSHA256(byteKey),
                HashAlgorithmType.Sha512 => new HMACSHA512(byteKey),
                HashAlgorithmType.Sha384 => new HMACSHA384(byteKey),
                _ => throw new AnonymizerException(AnonymizerErrorCode.CryptoHashFailed, "Hash function not supported."),
            };
            _cryptoHashSetting = cryptoHashSetting;
        }

        public byte[] Hash(byte[] input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return Hash(input, _hmac);
        }

        public byte[] Hash(Stream input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return Hash(input, _hmac);
        }

        public static byte[] Hash(byte[] input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public static byte[] Hash(Stream input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public string Hash(string input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return Hash(input, _hmac, encoding, _cryptoHashSetting.MatchInputStringLength);
        }

        public static string Hash(string input, HMAC hashAlgorithm, Encoding encoding = null, bool matchInputLength = false)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            encoding ??= Encoding.UTF8;

            var hash = hashAlgorithm.ComputeHash(encoding.GetBytes(input));

            if (matchInputLength)
            {
                return GenerateOutputOfSameLength(hash, input);
            }
            else
            {
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        /// <summary>
        /// Generates a string numeric-only output of the same length as the input string.
        /// </summary>
        private static string GenerateOutputOfSameLength(byte[] hash, string input)
        {
            var hashFloat = BitConverter.ToUInt32(hash, 0) / (float)uint.MaxValue;

            long orderOfMagnitude = (long)Math.Pow(10, input.Length - 1);
            if (orderOfMagnitude == 1)
            {
                orderOfMagnitude = 0;
            }
            long maxNumberGivenDigits = long.Parse(new string('9', input.Length));

            long hashLong = (long)(hashFloat * (maxNumberGivenDigits - orderOfMagnitude)) + orderOfMagnitude;

            return hashLong.ToString();
        }
    }
}
