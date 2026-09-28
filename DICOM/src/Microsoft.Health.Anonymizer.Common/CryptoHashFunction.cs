// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
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
        private readonly byte[] _key;
        private readonly HashAlgorithmType _hashType;
        private readonly bool _matchInputStringLength;

        public CryptoHashFunction(CryptoHashSetting cryptoHashSetting)
        {
            EnsureArg.IsNotNull(cryptoHashSetting, nameof(cryptoHashSetting));

            _key = cryptoHashSetting.GetCryptoHashByteKey();
            _hashType = cryptoHashSetting.CryptoHashType;
            _matchInputStringLength = cryptoHashSetting.MatchInputStringLength;
            using var hmac = CreateHmac();
        }

        public bool MatchInputStringLength => _matchInputStringLength;

        public byte[] Hash(byte[] input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            using var hmac = CreateHmac();
            return Hash(input, hmac);
        }

        public byte[] Hash(Stream input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            using var hmac = CreateHmac();
            return Hash(input, hmac);
        }

        public string Hash(string input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            if (_matchInputStringLength)
            {
                return HashToAlphabet(input, "0123456789", input.Length, encoding);
            }

            using var hmac = CreateHmac();
            return Hash(input, hmac, encoding);
        }

        public string HashToAlphabet(string input, string alphabet, int outputLength, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNullOrEmpty(alphabet, nameof(alphabet));

            if (outputLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outputLength));
            }

            if (alphabet.Length > byte.MaxValue + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(alphabet));
            }

            encoding ??= Encoding.UTF8;
            var inputBytes = encoding.GetBytes(input);
            var result = new StringBuilder(outputLength);
            var acceptanceLimit = (byte.MaxValue + 1) - ((byte.MaxValue + 1) % alphabet.Length);
            var counter = 0;
            while (result.Length < outputLength)
            {
                var counterBytes = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32LittleEndian(counterBytes, counter++);
                var blockInput = new byte[inputBytes.Length + counterBytes.Length];
                Buffer.BlockCopy(inputBytes, 0, blockInput, 0, inputBytes.Length);
                Buffer.BlockCopy(counterBytes, 0, blockInput, inputBytes.Length, counterBytes.Length);
                var block = Hash(blockInput);
                foreach (var value in block)
                {
                    if (value >= acceptanceLimit)
                    {
                        continue;
                    }

                    result.Append(alphabet[value % alphabet.Length]);
                    if (result.Length == outputLength)
                    {
                        break;
                    }
                }
            }

            return result.ToString();
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

        private static string GenerateOutputOfSameLength(byte[] hash, string input)
        {
            if (input.Length == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(input.Length);
            for (var index = 0; index < input.Length; index++)
            {
                result.Append((char)('0' + (hash[index % hash.Length] % 10)));
            }

            return result.ToString();
        }

        private HMAC CreateHmac()
        {
            return _hashType switch
            {
                HashAlgorithmType.Sha256 => new HMACSHA256(_key),
                HashAlgorithmType.Sha512 => new HMACSHA512(_key),
                HashAlgorithmType.Sha384 => new HMACSHA384(_key),
                _ => throw new AnonymizerException(AnonymizerErrorCode.CryptoHashFailed, "Hash function not supported."),
            };
        }
    }
}
