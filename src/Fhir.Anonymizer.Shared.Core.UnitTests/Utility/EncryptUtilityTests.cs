using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Utility
{
    public class EncryptUtilityTests
    {
        private byte[] Key => Encoding.UTF8.GetBytes("704ab12c8e3e46d4bea600ef62a6bec7"); 

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

        public static IEnumerable<object[]> GetTextDataToDecrypt()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, string.Empty };
            yield return new object[] { "qpxGp6T9DP7wB0EYPQwOYVrScQ/pq3c0D+JQ+hjnfkY=", "abc" };
            yield return new object[] { "GI99peR2SpPfcqEgzr7/z7gxYym6qyVPPzvmGc8o8SSwqMpCsW0CRj3v6ZsxFCef", "This is for test" };
            yield return new object[] { "JxNRbbxL6pYYpbrtHWZ+1gPTPcIVrLWmrugPiUR9d6k=", "!@)(*&%^!@#$%@" };
            yield return new object[] { "nQTtB/efLGpEqzOh/Pt4ZRlqZynO7gjePdu7LbLxH2LoqYJXAq6SQWkFeQ1SiqRM", "ͶΆΈΞξτϡϿῧῄᾴѶѾ" };
            yield return new object[] { "zrFYnZ2cIwcfmjCVybP1ZC+LaD7gwGXBHR2bZjuutzA=", "测试" };
        }

        public static IEnumerable<object[]> GetInvalidTextDataToDecrypt()
        {
            // Cipher text shorter than IV size
            yield return new object[] { "YWJj" };
            // Invalid base64 format
            yield return new object[] { "U29mdHdhcmUgdGVzdGluZyBpcyBhbiBpbnZlc3RpZ2F0aW9uIGNvbmR1Y3RlZCB0byBwcm92aWRlIGNvbmZpZGVuY2Uu=" };
            yield return new object[] { "QXMgdGhlIG51bWJlciBvZiBwb3NzaWJsZSB0ZXN0cyBmb3IgZXZlbiBzaW1wbGUgc29m**&&^^" };
        }

        [Theory]
        [MemberData(nameof(GetTextDataToEncrypt))]
        public void GivenAnOriginalText_WhenEncrypt_ResultShouldBeValidAndDecryptable(string originalText)
        {
            var cipherText = EncryptUtility.EncryptTextToBase64WithAes(originalText, Key);
            var plainText = EncryptUtility.DecryptTextFromBase64WithAes(cipherText, Key);
            Assert.Equal(originalText, plainText);
        }

        [Theory]
        [MemberData(nameof(GetTextDataToDecrypt))]
        public void GivenAnEncryptedBase64Text_WhenDecrypt_OriginalTextShouldBeReturned(string cipherText, string originalText)
        {
            var plainText = EncryptUtility.DecryptTextFromBase64WithAes(cipherText, Key);
            Assert.Equal(originalText, plainText);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTextDataToDecrypt))]
        public void GivenAInvalidBase64Text_WhenDecrypt_ExceptionShouldBeThrown(string cipherText)
        {
            Assert.Throws<FormatException>(() => EncryptUtility.DecryptTextFromBase64WithAes(cipherText, Key));
        }
    }
}
