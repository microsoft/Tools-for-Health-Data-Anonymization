// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class CryptoHashFunction
    {
        private readonly HMAC _hmac;

        public CryptoHashFunction(CryptoHashSetting cryptoHashSetting = null)
        {
            cryptoHashSetting ??= new CryptoHashSetting();
            _hmac = cryptoHashSetting.CryptoHashKey == null ? new HMACSHA256() : new HMACSHA256(Encoding.UTF8.GetBytes(cryptoHashSetting.CryptoHashKey));
        }

        public byte[] ComputeHmacSHA256Hash(byte[] input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _hmac.ComputeHash(input);
        }

        public byte[] ComputeHmacSHA256Hash(Stream input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _hmac.ComputeHash(input);
        }

        public byte[] ComputeHmacSHA256Hash(string input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            encoding ??= Encoding.UTF8;
            return ComputeHmacSHA256Hash(encoding.GetBytes(input));
        }

        public FixedLengthString ComputeHmacSHA256Hash(FixedLengthString input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            var hashData = ComputeHmacSHA256Hash(input.ToString());

            return new FixedLengthString(input.GetLength(), string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        public byte[] ComputeHmacHash(byte[] input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public byte[] ComputeHmacHash(string input, HMAC hashAlgorithm, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            encoding ??= Encoding.UTF8;
            return hashAlgorithm.ComputeHash(encoding.GetBytes(input));
        }

        public byte[] ComputeHmacHash(Stream input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public FixedLengthString ComputeHmacHash(FixedLengthString input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            var hashData = ComputeHmacHash(input.ToString(), hashAlgorithm);

            input.SetString(string.Concat(hashData.Select(b => b.ToString("x2"))));
            return input;
        }
    }
}
