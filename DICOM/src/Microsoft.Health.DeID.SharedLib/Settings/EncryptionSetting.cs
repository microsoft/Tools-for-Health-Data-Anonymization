// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;

namespace Microsoft.Health.DeID.SharedLib.Settings
{
    public class EncryptionSetting
    {
        public string EncryptKey { get; set; }

        public string PublicKey { get; set; }

        public string PrivateKey { get; set; }

        public void Validate()
        {
            using Aes aes = Aes.Create();
            var encryptKeySize = Encoding.UTF8.GetByteCount(EncryptKey) * 8;
            if (!IsValidKeySize(encryptKeySize, aes.LegalKeySizes))
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.InvalidDeIdSettings, $"Invalid encrypt key size : {encryptKeySize} bits! Please provide key sizes of 128, 192 or 256 bits.");
            }
        }

        private bool IsValidKeySize(int bitLength, KeySizes[] validSizes)
        {
            if (validSizes == null)
            {
                return false;
            }

            for (int i = 0; i < validSizes.Length; i++)
            {
                for (int j = validSizes[i].MinSize; j <= validSizes[i].MaxSize; j += validSizes[i].SkipSize)
                {
                    if (j == bitLength)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
