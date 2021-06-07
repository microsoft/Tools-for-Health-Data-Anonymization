// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Health.DeID.SharedLib.Settings;

namespace Microsoft.Health.Dicom.DeID.SharedLib
{
    public class EncryptFunction
    {
        // AES Initialization Vector length is 16 bytes
        private const int AesIvSize = 16;
        private byte[] _aesKey;
        private byte[] _privateKey;
        private byte[] _publicKey;

        public EncryptFunction(EncryptionSetting encryptionSetting = null)
        {
            encryptionSetting ??= new EncryptionSetting();
            encryptionSetting.Validate();

            _aesKey = Encoding.UTF8.GetBytes(encryptionSetting.EncryptKey ?? Guid.NewGuid().ToString("N"));
            _privateKey = Encoding.UTF8.GetBytes(encryptionSetting.PrivateKey ?? string.Empty);
            _publicKey = Encoding.UTF8.GetBytes(encryptionSetting.PublicKey ?? string.Empty);
        }

        public byte[] EncryptContentWithAES(string plainText, Encoding encoding = null)
        {
            if (plainText == string.Empty)
            {
                return new byte[] { };
            }

            if (plainText == null)
            {
                return null;
            }

            encoding ??= Encoding.UTF8;
            return EncryptContentWithAES(encoding.GetBytes(plainText));
        }

        public byte[] EncryptContentWithAES(Stream plainStream)
        {
            if (plainStream == null)
            {
                return null;
            }

            return EncryptContentWithAES(StreamToByte(plainStream));
        }

        public byte[] EncryptContentWithAES(byte[] plainBytes)
        {
            if (plainBytes == null)
            {
                return null;
            }

            /* Create AES encryptor:
             * Mode: CBC
             * Block size: 16 bytes
             * Acceptable key sizes： [128, 192, 256]
             */
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

        public byte[] DecryptContentWithAES(string cipherText)
        {
            if (cipherText == string.Empty)
            {
                return new byte[] { };
            }

            if (cipherText == null)
            {
                return null;
            }

            var byteData = Encoding.UTF8.GetBytes(cipherText);
            return DecryptContentWithAES(byteData);
        }

        public byte[] DecryptContentWithAES(Stream cipherStream)
        {
            if (cipherStream == null)
            {
                return null;
            }

            return DecryptContentWithAES(StreamToByte(cipherStream));
        }

        public byte[] DecryptContentWithAES(byte[] cipherBytes)
        {
            if (cipherBytes == null)
            {
                return null;
            }

            if (cipherBytes.Length == 0)
            {
                return cipherBytes;
            }

            // Extract IV info from base64 text

            if (cipherBytes.Length < AesIvSize)
            {
                throw new FormatException($"The input base64Text for decryption should not be less than {AesIvSize} bytes length!");
            }

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

        public byte[] EncryptContentWithRSA(string plainText)
        {
            if (plainText == null)
            {
                return null;
            }

            return EncryptContentWithRSA(Encoding.UTF8.GetBytes(plainText));
        }

        public byte[] EncryptContentWithRSA(Stream plainStream)
        {
            if (plainStream == null)
            {
                return null;
            }

            byte[] result;
            using (var streamReader = new MemoryStream())
            {
                plainStream.CopyTo(streamReader);
                result = streamReader.ToArray();
            }

            return EncryptContentWithRSA(result);
        }

        public byte[] EncryptContentWithRSA(byte[] plainBytes, bool doPadding = true)
        {
            if (plainBytes == null)
            {
                return null;
            }

            try
            {
                byte[] encryptedData;
                using (RSACryptoServiceProvider rSA = new RSACryptoServiceProvider())
                {
                    rSA.ImportRSAPublicKey(_publicKey, out _);
                    encryptedData = rSA.Encrypt(plainBytes, doPadding);
                }

                return encryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public byte[] DecryptContentWithRSA(byte[] cipherBytes, bool doPadding = true)
        {
            if (cipherBytes == null)
            {
                return null;
            }

            try
            {
                byte[] encryptedData;
                using (RSACryptoServiceProvider rSA = new RSACryptoServiceProvider())
                {
                    rSA.ImportRSAPrivateKey(_privateKey, out _);
                    encryptedData = rSA.Decrypt(cipherBytes, doPadding);
                }

                return encryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public byte[] DecryptContentWithRSA(string cipherText, bool doPadding = true)
        {
            if (cipherText == string.Empty)
            {
                return new byte[] { };
            }

            if (cipherText == null)
            {
                return null;
            }

            return DecryptContentWithRSA(Convert.FromBase64String(cipherText), doPadding);
        }

        public byte[] DecryptContentWithRSA(Stream cipherStream, bool doPadding = true)
        {
            if (cipherStream == null)
            {
                return null;
            }

            return DecryptContentWithRSA(StreamToByte(cipherStream), doPadding);
        }

        private byte[] StreamToByte(Stream inputStream)
        {
            if (inputStream == null)
            {
                return null;
            }

            inputStream.Position = 0;
            using var streamReader = new MemoryStream();
            inputStream.CopyTo(streamReader);
            return streamReader.ToArray();
        }
    }
}
