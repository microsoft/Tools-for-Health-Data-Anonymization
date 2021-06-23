// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib.Exceptions;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class EncryptFunction
    {
        // AES Initialization Vector length is 16 bytes
        private const int AesIvSize = 16;
        private readonly byte[] _aesKey;

        public EncryptFunction(EncryptSetting encryptionSetting = null)
        {
            encryptionSetting ??= new EncryptSetting();
            encryptionSetting.Validate();

            _aesKey = Encoding.UTF8.GetBytes(encryptionSetting.EncryptKey);
        }

        public byte[] Encrypt(string plainText, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(plainText, nameof(plainText));

            if (plainText == string.Empty)
            {
                return new byte[] { };
            }

            encoding ??= Encoding.UTF8;
            return Encrypt(encoding.GetBytes(plainText));
        }

        public byte[] Encrypt(Stream plainStream)
        {
            EnsureArg.IsNotNull(plainStream, nameof(plainStream));

            return Encrypt(StreamToByte(plainStream));
        }

        public byte[] Encrypt(byte[] plainBytes)
        {
            EnsureArg.IsNotNull(plainBytes, nameof(plainBytes));

            /* Create AES encryptor:
             * Mode: CBC
             * Block size: 16 bytes
             * Acceptable key sizes： [128, 192, 256]
             */
            try
            {
                using Aes aes = Aes.Create();
                byte[] iv = aes.IV;
                aes.Key = _aesKey;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Concat IV to encrpted bytes
                byte[] result = new byte[AesIvSize + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, result, 0, AesIvSize);
                Buffer.BlockCopy(encryptedBytes, 0, result, AesIvSize, encryptedBytes.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.EncryptFailed, "Encrypt data failed.", ex);
            }
        }

        public byte[] Decrypt(string cipherText, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(cipherText, nameof(cipherText));

            if (cipherText == string.Empty)
            {
                return new byte[] { };
            }

            encoding ??= Encoding.UTF8;
            var byteData = encoding.GetBytes(cipherText);
            return Decrypt(byteData);
        }

        public byte[] Decrypt(Stream cipherStream)
        {
            EnsureArg.IsNotNull(cipherStream, nameof(cipherStream));

            return Decrypt(StreamToByte(cipherStream));
        }

        public byte[] Decrypt(byte[] cipherBytes)
        {
            EnsureArg.IsNotNull(cipherBytes, nameof(cipherBytes));

            if (cipherBytes.Length == 0)
            {
                return cipherBytes;
            }

            // Extract IV info from base64 text

            if (cipherBytes.Length < AesIvSize)
            {
                throw new FormatException($"The input text for decryption should not be less than {AesIvSize} bytes length!");
            }

            try
            {
                var iv = new byte[16];
                Buffer.BlockCopy(cipherBytes, 0, iv, 0, AesIvSize);
                var encryptedBytes = new byte[cipherBytes.Length - AesIvSize];
                Buffer.BlockCopy(cipherBytes, AesIvSize, encryptedBytes, 0, encryptedBytes.Length);

                // Get decryptor
                using Aes aes = Aes.Create();
                aes.Key = _aesKey;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            }
            catch (Exception ex)
            {
                throw new DeIDFunctionException(DeIDFunctionErrorCode.EncryptFailed, "Decrypt data failed.", ex);
            }
        }

        private byte[] StreamToByte(Stream inputStream)
        {
            EnsureArg.IsNotNull(inputStream, nameof(inputStream));

            inputStream.Position = 0;
            using var streamReader = new MemoryStream();
            inputStream.CopyTo(streamReader);
            return streamReader.ToArray();
        }
    }
}
