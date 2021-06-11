// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Xunit;

namespace De.ID.Function.Shared.UnitTests
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
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { "abc" };
            yield return new object[] { "This is for test" };
            yield return new object[] { "!@)(*&%^!@#$%@" };
            yield return new object[] { "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { "测试" };
        }

        public static IEnumerable<object[]> GetBytesDataToEncrypt()
        {
            yield return new object[] { null };
            yield return new object[] { Encoding.UTF8.GetBytes(string.Empty) };
            yield return new object[] { Encoding.UTF8.GetBytes("abc") };
            yield return new object[] { Encoding.UTF8.GetBytes("This is for test") };
            yield return new object[] { Encoding.UTF8.GetBytes("!@)(*&%^!@#$%@") };
            yield return new object[] { Encoding.UTF8.GetBytes("ͶΆΈΞξτϡϿῧῄᾴѶѾ") };
            yield return new object[] { Encoding.UTF8.GetBytes("测试") };
        }

        public static IEnumerable<object[]> GetStreamDataToEncrypt()
        {
            yield return new object[] { null };
            yield return new object[] { new MemoryStream() };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("abc")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("This is for test")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("!@)(*&%^!@#$%@")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("ͶΆΈΞξτϡϿῧῄᾴѶѾ")) };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("测试")) };
        }

        public static IEnumerable<object[]> GetBytesDataToDecryptUsingAES()
        {
            yield return new object[] { null, null };
            yield return new object[] { Convert.FromBase64String(string.Empty), string.Empty };
            yield return new object[] { Convert.FromBase64String("qpxGp6T9DP7wB0EYPQwOYVrScQ/pq3c0D+JQ+hjnfkY="), "abc" };
            yield return new object[] { Convert.FromBase64String("GI99peR2SpPfcqEgzr7/z7gxYym6qyVPPzvmGc8o8SSwqMpCsW0CRj3v6ZsxFCef"), "This is for test" };
            yield return new object[] { Convert.FromBase64String("JxNRbbxL6pYYpbrtHWZ+1gPTPcIVrLWmrugPiUR9d6k="), "!@)(*&%^!@#$%@" };
            yield return new object[] { Convert.FromBase64String("nQTtB/efLGpEqzOh/Pt4ZRlqZynO7gjePdu7LbLxH2LoqYJXAq6SQWkFeQ1SiqRM"), "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { Convert.FromBase64String("zrFYnZ2cIwcfmjCVybP1ZC+LaD7gwGXBHR2bZjuutzA="), "测试" };
        }

        public static IEnumerable<object[]> GetStreamDataToDecryptUsingAES()
        {
            yield return new object[] { null, null };
            yield return new object[] { new MemoryStream(), string.Empty };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("qpxGp6T9DP7wB0EYPQwOYVrScQ/pq3c0D+JQ+hjnfkY=")), "abc" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("GI99peR2SpPfcqEgzr7/z7gxYym6qyVPPzvmGc8o8SSwqMpCsW0CRj3v6ZsxFCef")), "This is for test" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("JxNRbbxL6pYYpbrtHWZ+1gPTPcIVrLWmrugPiUR9d6k=")), "!@)(*&%^!@#$%@" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("nQTtB/efLGpEqzOh/Pt4ZRlqZynO7gjePdu7LbLxH2LoqYJXAq6SQWkFeQ1SiqRM")), "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("zrFYnZ2cIwcfmjCVybP1ZC+LaD7gwGXBHR2bZjuutzA=")), "测试" };
        }

        public static IEnumerable<object[]> GetBytesDataToDecryptUsingRSA()
        {
            yield return new object[] { null, null };
            yield return new object[] { Convert.FromBase64String("gFFwULcgN3ZknzEXQKoFZa6ccflcOEzt675LS3e0Wv/A3PobHq8E04CUNK6KVrqJDnrF4Mas1/qLI2JNL3FCzDPcHMEUp4TscZS5s6zYMFSOB7dQfasUV45H74FnT58/b1qGvNYMAAyzMgXXF2+tnGfNoCZMK5JryAMEvL0qaxY="), string.Empty };
            yield return new object[] { Convert.FromBase64String("ijJ2ZaK8yoPfoeg6CFuxNJ3kmuahIXlFRjLNsQWD4GKbCImW1eLViBBArZ5un8xErpD9DoRyjdVRIKNeQGq//wJrPCR/lEOk6k2V3Xxx+sB7OH+HSHufSEhL2UMXqghUQNiwEQ59NysQK6fpq9a5n7fzgAd2fOfmTwQGngtxj0Q="), "abc" };
            yield return new object[] { Convert.FromBase64String("eSiv9qcZj4ma50T52JGVxX40U3fJOHjPnjQAMyX6MGHEn0gSAQi8SCpT4eVDg7DcPmGasOaAxiEFSOkS+Z/kSQlQKeDatQrKDxA9LcOTzA6H4dYmep6pUIvLhmyouZ0CBcg+J4syxFI35DbhnYFXhd5mQy8tWJybUaPhK90t3+s="), "This is for test" };
            yield return new object[] { Convert.FromBase64String("NmaSNhl6uE3976EhND1l5hHS2ukNpiQzjVqN9exokh/OXF6AIbQjo5exiEFQJLHDiETX6apdRfGoGXWXMHVaom92CJ1hP9oksd4n6MkQyFxg2aArKP/AsT4C6TA5BCfW4Srw04dQfS+e7JA0VEvh5MnggdMlEmL6fx7gHVCberM="), "!@)(*&%^!@#$%@" };
            yield return new object[] { Convert.FromBase64String("eZk0YqKeQx5z40WUjpjKDOCFGzQ4oJYw80CpIE8GL1RdGiOXomPgv3pxnFdgv+uhzC79CocFABagoqa2v0F5/9UzGxx54v2fYM/SC5dmTzU2XbbPVPs3lVr9KYuskDbmR+HlhWvKl0fMQgXcnYO39wKgHnbamcRuiPYrvowI8zM="), "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { Convert.FromBase64String("dcxR73jyAw3Q6133hGE04EYA5wuz6BoyH6nqFK2OOWXmh9DDjqGmS4ivAdUrp99q+qpCoyOWktXiN3GVVcBRUtxK6PTHZAb6ufWQLbYFFEx9TlFzUTEYY5LZ6GyFqNrgSMTCydHB/ouf3F5/74z+/dz2KMZ4pcEuJGIhEvN/5qc="), "测试" };
        }

        public static IEnumerable<object[]> GetStreamDataToDecryptUsingRSA()
        {
            yield return new object[] { null, null };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("gFFwULcgN3ZknzEXQKoFZa6ccflcOEzt675LS3e0Wv/A3PobHq8E04CUNK6KVrqJDnrF4Mas1/qLI2JNL3FCzDPcHMEUp4TscZS5s6zYMFSOB7dQfasUV45H74FnT58/b1qGvNYMAAyzMgXXF2+tnGfNoCZMK5JryAMEvL0qaxY=")), string.Empty };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("ijJ2ZaK8yoPfoeg6CFuxNJ3kmuahIXlFRjLNsQWD4GKbCImW1eLViBBArZ5un8xErpD9DoRyjdVRIKNeQGq//wJrPCR/lEOk6k2V3Xxx+sB7OH+HSHufSEhL2UMXqghUQNiwEQ59NysQK6fpq9a5n7fzgAd2fOfmTwQGngtxj0Q=")), "abc" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("eSiv9qcZj4ma50T52JGVxX40U3fJOHjPnjQAMyX6MGHEn0gSAQi8SCpT4eVDg7DcPmGasOaAxiEFSOkS+Z/kSQlQKeDatQrKDxA9LcOTzA6H4dYmep6pUIvLhmyouZ0CBcg+J4syxFI35DbhnYFXhd5mQy8tWJybUaPhK90t3+s=")), "This is for test" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("NmaSNhl6uE3976EhND1l5hHS2ukNpiQzjVqN9exokh/OXF6AIbQjo5exiEFQJLHDiETX6apdRfGoGXWXMHVaom92CJ1hP9oksd4n6MkQyFxg2aArKP/AsT4C6TA5BCfW4Srw04dQfS+e7JA0VEvh5MnggdMlEmL6fx7gHVCberM=")), "!@)(*&%^!@#$%@" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("eZk0YqKeQx5z40WUjpjKDOCFGzQ4oJYw80CpIE8GL1RdGiOXomPgv3pxnFdgv+uhzC79CocFABagoqa2v0F5/9UzGxx54v2fYM/SC5dmTzU2XbbPVPs3lVr9KYuskDbmR+HlhWvKl0fMQgXcnYO39wKgHnbamcRuiPYrvowI8zM=")), "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { new MemoryStream(Convert.FromBase64String("dcxR73jyAw3Q6133hGE04EYA5wuz6BoyH6nqFK2OOWXmh9DDjqGmS4ivAdUrp99q+qpCoyOWktXiN3GVVcBRUtxK6PTHZAb6ufWQLbYFFEx9TlFzUTEYY5LZ6GyFqNrgSMTCydHB/ouf3F5/74z+/dz2KMZ4pcEuJGIhEvN/5qc=")), "测试" };
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
            var cipherText = encryptFunction.EncryptContentWithAES(originalText);
            var plainText = encryptFunction.DecryptContentWithAES(cipherText);
            Assert.Equal(originalText, plainText == null ? null : plainText.Length == 0 ? string.Empty : Encoding.UTF8.GetString(plainText));
        }

        [Theory]
        [MemberData(nameof(GetBytesDataToEncrypt))]
        public void GivenAnOriginalBytes_WhenEncrypt_ResultShouldBeValidAndDecryptable(byte[] originalBytes)
        {
            var cipherText = encryptFunction.EncryptContentWithAES(originalBytes);
            var plainText = encryptFunction.DecryptContentWithAES(cipherText);
            Assert.Equal(originalBytes, plainText);
        }

        [Theory]
        [MemberData(nameof(GetStreamDataToEncrypt))]
        public void GivenAnOriginalStream_WhenEncrypt_ResultShouldBeValidAndDecryptable(Stream originalStream)
        {
            var cipherText = encryptFunction.EncryptContentWithAES(originalStream);
            var plainText = encryptFunction.DecryptContentWithAES(cipherText);
            Assert.Equal(StreamToByte(originalStream), plainText);
        }

        [Theory]
        [MemberData(nameof(GetBytesDataToDecryptUsingAES))]
        public void GivenAnEncryptedBytes_WhenDecrypt_OriginalTextShouldBeReturned(byte[] cipherText, string originalText)
        {
            var plainText = encryptFunction.DecryptContentWithAES(cipherText);
            Assert.Equal(originalText, plainText == null ? null : plainText.Length == 0 ? string.Empty : Encoding.UTF8.GetString(plainText));
        }

        [Theory]
        [MemberData(nameof(GetStreamDataToDecryptUsingAES))]
        public void GivenAnEncryptedStream_WhenDecrypt_OriginalTextShouldBeReturned(Stream cipherText, string originalText)
        {
            var plainText = encryptFunction.DecryptContentWithAES(cipherText);
            Assert.Equal(originalText, plainText == null ? null : plainText.Length == 0 ? string.Empty : Encoding.UTF8.GetString(plainText));
        }

        [Theory]
        [MemberData(nameof(GetInvalidFormatTextDataToDecrypt))]
        public void GivenAInvalidForamtText_WhenDecrypt_ExceptionShouldBeThrown(string cipherText)
        {
            Assert.Throws<FormatException>(() => encryptFunction.DecryptContentWithAES(cipherText));
        }

        [Theory]
        [MemberData(nameof(GetInvalidCryptoGraphicTextDataToDecrypt))]
        public void GivenAInvalidCryptoGraphicText_WhenDecrypt_ExceptionShouldBeThrown(string cipherText)
        {
            Assert.Throws<CryptographicException>(() => encryptFunction.DecryptContentWithAES(cipherText));
        }

        private static byte[] StreamToByte(Stream inputStream)
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
