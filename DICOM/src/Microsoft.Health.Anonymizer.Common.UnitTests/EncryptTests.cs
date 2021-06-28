// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Settings;
using Xunit;

namespace Microsoft.Health.Anonymizer.Common.UnitTests
{
    public class EncryptTests
    {
        private readonly EncryptFunction encryptFunction;

        public EncryptTests()
        {
            encryptFunction = new EncryptFunction(new EncryptSetting() { EncryptKey = Key });
        }

        private string Key => "704ab12c8e3e46d4bea600ef62a6bec7";

        public static IEnumerable<object[]> GetTextDataToEncrypt()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "abc" };
            yield return new object[] { "This is for test" };
            yield return new object[] { "!@)(*&%^!@#$%@" };
            yield return new object[] { "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { "测试" };
        }

        public static IEnumerable<object[]> GetBytesDataToEncrypt()
        {
            yield return new object[] { Encoding.UTF8.GetBytes(string.Empty) };
            yield return new object[] { Encoding.UTF8.GetBytes("abc") };
            yield return new object[] { Encoding.UTF8.GetBytes("This is for test") };
            yield return new object[] { Encoding.UTF8.GetBytes("!@)(*&%^!@#$%@") };
            yield return new object[] { Encoding.UTF8.GetBytes("ͶΆΈΞξτϡϿῧῄᾴѶѾ") };
            yield return new object[] { Encoding.UTF8.GetBytes("测试") };
        }

        public static IEnumerable<object[]> GetStreamDataToEncrypt()
        {
            yield return new object[] { new MemoryStream() };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("abc")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("This is for test")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("!@)(*&%^!@#$%@")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("ͶΆΈΞξτϡϿῧῄᾴѶѾ")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("测试")) };
        }

        public static IEnumerable<object[]> GetBytesDataToDecryptUsingAES()
        {
            yield return new object[] { Convert.FromBase64String(string.Empty), string.Empty };
            yield return new object[] { Convert.FromBase64String("qpxGp6T9DP7wB0EYPQwOYVrScQ/pq3c0D+JQ+hjnfkY="), "abc" };
            yield return new object[] { Convert.FromBase64String("GI99peR2SpPfcqEgzr7/z7gxYym6qyVPPzvmGc8o8SSwqMpCsW0CRj3v6ZsxFCef"), "This is for test" };
            yield return new object[] { Convert.FromBase64String("JxNRbbxL6pYYpbrtHWZ+1gPTPcIVrLWmrugPiUR9d6k="), "!@)(*&%^!@#$%@" };
            yield return new object[] { Convert.FromBase64String("nQTtB/efLGpEqzOh/Pt4ZRlqZynO7gjePdu7LbLxH2LoqYJXAq6SQWkFeQ1SiqRM"), "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { Convert.FromBase64String("zrFYnZ2cIwcfmjCVybP1ZC+LaD7gwGXBHR2bZjuutzA="), "测试" };
        }

        public static IEnumerable<object[]> GetStreamDataToDecryptUsingAES()
        {
            yield return new object[] { new MemoryStream(), string.Empty };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("qpxGp6T9DP7wB0EYPQwOYVrScQ/pq3c0D+JQ+hjnfkY=")), "abc" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("GI99peR2SpPfcqEgzr7/z7gxYym6qyVPPzvmGc8o8SSwqMpCsW0CRj3v6ZsxFCef")), "This is for test" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("JxNRbbxL6pYYpbrtHWZ+1gPTPcIVrLWmrugPiUR9d6k=")), "!@)(*&%^!@#$%@" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("nQTtB/efLGpEqzOh/Pt4ZRlqZynO7gjePdu7LbLxH2LoqYJXAq6SQWkFeQ1SiqRM")), "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("zrFYnZ2cIwcfmjCVybP1ZC+LaD7gwGXBHR2bZjuutzA=")), "测试" };
        }

        public static IEnumerable<object[]> GetInvalidFormatTextDataToDecrypt()
        {
            // Cipher text shorter than IV size
            yield return new object[] { "YWJj" };
        }

        public static IEnumerable<object[]> GetInvalidCryptoGraphicTextDataToDecrypt()
        {
            // Invalid cipher format
            yield return new object[] { "U29mdHdhcmUgdGVzdGluZyBpcyBhbiBp" };
            yield return new object[] { "QXMgdGhlIG51bWJlciBvZiBwb3NzaWJsZSB0ZXN0cyBmb3IgZXZlbiBzaW1wbGUgc29m**&&^^" };
        }

        [Theory]
        [MemberData(nameof(GetTextDataToEncrypt))]
        public void GivenAnOriginalText_WhenEncrypt_ResultShouldBeValidAndDecryptable(string originalText)
        {
            var cipherText = encryptFunction.Encrypt(originalText);
            var plainText = encryptFunction.Decrypt(cipherText);
            Assert.Equal(originalText, plainText == null ? null : plainText.Length == 0 ? string.Empty : Encoding.UTF8.GetString(plainText));
        }

        [Theory]
        [MemberData(nameof(GetBytesDataToEncrypt))]
        public void GivenAnOriginalBytes_WhenEncrypt_ResultShouldBeValidAndDecryptable(byte[] originalBytes)
        {
            var cipherText = encryptFunction.Encrypt(originalBytes);
            var plainText = encryptFunction.Decrypt(cipherText);
            Assert.Equal(originalBytes, plainText);
        }

        [Theory]
        [MemberData(nameof(GetStreamDataToEncrypt))]
        public void GivenAnOriginalStream_WhenEncrypt_ResultShouldBeValidAndDecryptable(Stream originalStream)
        {
            var cipherText = encryptFunction.Encrypt(originalStream);
            var plainText = encryptFunction.Decrypt(cipherText);
            Assert.Equal(StreamToByte(originalStream), plainText);
        }

        [Theory]
        [MemberData(nameof(GetBytesDataToDecryptUsingAES))]
        public void GivenAnEncryptedBytes_WhenDecrypt_OriginalTextShouldBeReturned(byte[] cipherText, string originalText)
        {
            var plainText = encryptFunction.Decrypt(cipherText);
            Assert.Equal(originalText, plainText == null ? null : plainText.Length == 0 ? string.Empty : Encoding.UTF8.GetString(plainText));
        }

        [Theory]
        [MemberData(nameof(GetStreamDataToDecryptUsingAES))]
        public void GivenAnEncryptedStream_WhenDecrypt_OriginalTextShouldBeReturned(Stream cipherText, string originalText)
        {
            var plainText = encryptFunction.Decrypt(cipherText);
            Assert.Equal(originalText, plainText == null ? null : plainText.Length == 0 ? string.Empty : Encoding.UTF8.GetString(plainText));
        }

        [Theory]
        [MemberData(nameof(GetInvalidFormatTextDataToDecrypt))]
        public void GivenAInvalidForamtText_WhenDecrypt_ExceptionShouldBeThrown(string cipherText)
        {
            Assert.Throws<FormatException>(() => encryptFunction.Decrypt(cipherText));
        }

        [Theory]
        [MemberData(nameof(GetInvalidCryptoGraphicTextDataToDecrypt))]
        public void GivenAInvalidCryptoGraphicText_WhenDecrypt_ExceptionShouldBeThrown(string cipherText)
        {
            Assert.Throws<AnonymizerException>(() => encryptFunction.Decrypt(cipherText));
        }

        private static byte[] StreamToByte(Stream inputStream)
        {
            if (inputStream == null)
            {
                return null;
            }

            inputStream.Position = 0;
            using var memoryStream = new MemoryStream();
            inputStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
