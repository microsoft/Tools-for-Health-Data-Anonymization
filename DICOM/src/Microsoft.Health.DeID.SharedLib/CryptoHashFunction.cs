// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.DeID.SharedLib.Models;
using Microsoft.Health.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class CryptoHashFunction
    {
        private readonly HMAC _hmac;

        public CryptoHashFunction(CryptoHashSetting cryptoHashSetting = null)
        {
            cryptoHashSetting ??= new CryptoHashSetting();
            byte[] byteKey = cryptoHashSetting.CryptoHashKey == null ? null : Encoding.UTF8.GetBytes(cryptoHashSetting.CryptoHashKey);
            switch (cryptoHashSetting.CryptoHashType)
            {
                case CryptoHashType.HMACSHA1:
                    _hmac = byteKey == null ? new HMACSHA1() : new HMACSHA1(byteKey);
                    break;
                case CryptoHashType.HMACSHA256:
                    _hmac = byteKey == null ? new HMACSHA256() : new HMACSHA256(byteKey);
                    break;
                case CryptoHashType.HMACSHA512:
                    _hmac = byteKey == null ? new HMACSHA512() : new HMACSHA512(byteKey);
                    break;
                default:
                    break;
            }
        }

        public byte[] ComputeHmacHash(byte[] input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _hmac.ComputeHash(input);
        }

        public byte[] ComputeHmacHash(Stream input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return _hmac.ComputeHash(input);
        }

        public byte[] ComputeHmacHash(string input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            encoding ??= Encoding.UTF8;
            return ComputeHmacHash(encoding.GetBytes(input));
        }

        public FixedLengthString ComputeHmacHash(FixedLengthString input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            var hashData = ComputeHmacHash(input.ToString(), encoding);

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

        public FixedLengthString ComputeHmacHash(FixedLengthString input, HMAC hashAlgorithm, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            var hashData = ComputeHmacHash(input.ToString(), hashAlgorithm, encoding);

            input.SetString(string.Concat(hashData.Select(b => b.ToString("x2"))));
            return input;
        }
    }
}
