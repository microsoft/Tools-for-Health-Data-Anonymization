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
        private const int MaximumExpandedOutputLength = 4096;
        private const int MaximumExpansionBlocks = 1024;

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
            ValidateExpandedOutputLength(outputLength);

            encoding ??= Encoding.UTF8;
            return GenerateOutputFromAlphabet(encoding.GetBytes(input), alphabet, outputLength, Hash);
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

            if (matchInputLength)
            {
                ValidateExpandedOutputLength(input.Length);
                return GenerateOutputFromAlphabet(
                    encoding.GetBytes(input),
                    "0123456789",
                    input.Length,
                    hashAlgorithm.ComputeHash);
            }
            else
            {
                var hash = hashAlgorithm.ComputeHash(encoding.GetBytes(input));
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        private static string GenerateOutputFromAlphabet(byte[] input, string alphabet, int outputLength, Func<byte[], byte[]> hash)
        {
            ValidateExpandedOutputLength(outputLength);

            if (alphabet.Length == 0 || alphabet.Length > byte.MaxValue + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(alphabet));
            }

            if (outputLength == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(outputLength);
            var acceptanceLimit = (byte.MaxValue + 1) - ((byte.MaxValue + 1) % alphabet.Length);
            var blockInput = new byte[input.Length + sizeof(int)];
            Buffer.BlockCopy(input, 0, blockInput, 0, input.Length);
            var counter = 0;
            while (result.Length < outputLength)
            {
                if (counter == MaximumExpansionBlocks)
                {
                    throw new AnonymizerException(
                        AnonymizerErrorCode.CryptoHashFailed,
                        $"Hash output expansion exceeds the supported limit of {MaximumExpansionBlocks} HMAC blocks.");
                }

                BinaryPrimitives.WriteInt32LittleEndian(blockInput.AsSpan(input.Length), counter++);
                foreach (var value in hash(blockInput))
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

        private static void ValidateExpandedOutputLength(int outputLength)
        {
            if (outputLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outputLength));
            }

            if (outputLength > MaximumExpandedOutputLength)
            {
                throw new AnonymizerException(
                    AnonymizerErrorCode.CryptoHashFailed,
                    $"Hash output length exceeds the supported limit of {MaximumExpandedOutputLength} characters.");
            }
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
