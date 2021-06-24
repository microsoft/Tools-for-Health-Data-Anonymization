// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class CryptoHashFunction
    {
        private readonly HMAC _hmac;

        public CryptoHashFunction(CryptoHashSetting cryptoHashSetting = null)
        {
            cryptoHashSetting ??= new CryptoHashSetting();
            byte[] byteKey = cryptoHashSetting.CryptoHashKey == null ? null : Encoding.UTF8.GetBytes(cryptoHashSetting.CryptoHashKey);
            _hmac = cryptoHashSetting.CryptoHashType switch
            {
                HashAlgorithmType.Md5 => byteKey == null ? new HMACMD5() : new HMACMD5(byteKey),
                HashAlgorithmType.Sha1 => byteKey == null ? new HMACSHA1() : new HMACSHA1(byteKey),
                HashAlgorithmType.Sha256 => byteKey == null ? new HMACSHA256() : new HMACSHA256(byteKey),
                HashAlgorithmType.Sha512 => byteKey == null ? new HMACSHA512() : new HMACSHA512(byteKey),
                HashAlgorithmType.Sha384 => byteKey == null ? new HMACSHA384() : new HMACSHA384(byteKey),
                _ => throw new DeIDFunctionException(DeIDFunctionErrorCode.CryptoHashFailed, "Hash function not supported."),
            };
        }

        public byte[] Hash(byte[] input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _hmac.ComputeHash(input);
        }

        public byte[] Hash(Stream input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _hmac.ComputeHash(input);
        }

        public byte[] Hash(string input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            encoding ??= Encoding.UTF8;
            return Hash(encoding.GetBytes(input));
        }

        public FixedLengthString Hash(FixedLengthString input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            var hashData = Hash(input.ToString(), encoding);

            input.SetString(string.Concat(hashData.Select(b => b.ToString("x2"))));
            return input;
        }

        public byte[] Hash(byte[] input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public byte[] Hash(string input, HMAC hashAlgorithm, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            encoding ??= Encoding.UTF8;
            return hashAlgorithm.ComputeHash(encoding.GetBytes(input));
        }

        public byte[] Hash(Stream input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public FixedLengthString Hash(FixedLengthString input, HMAC hashAlgorithm, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            var hashData = Hash(input.ToString(), hashAlgorithm, encoding);

            input.SetString(string.Concat(hashData.Select(b => b.ToString("x2"))));
            return input;
        }
    }
}
