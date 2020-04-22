using System.Collections.Generic;
using Fhir.Anonymizer.Core.Utility;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Utility
{
    public class CryptoHashUtilityTests
    {
        private const string TestHashKey = "123";
        public static IEnumerable<object[]> GetHmacHashData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, string.Empty }; 
            yield return new object[] { "abc", "8f16771f9f8851b26f4d460fa17de93e2711c7e51337cb8a608a0f81e1c1b6ae" };
            yield return new object[] { "&*^%$@()=-,/", "33f6f7d6b3602bf5354dcb4b8d988982602349355f50f86798d8ce1ffd61521b" };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-",
                "352b5a4af5adb81fa616c2a5b5c492d0b0b544c188a9aa003767a2b5efbd1478" };
        }

        [Theory]
        [MemberData(nameof(GetHmacHashData))]
        public void GivenAString_WhenComputeHmac_CorrectHashShouldBeReturned(string input, string expectedHash)
        {
            string hash = CryptoHashUtility.ComputeHmacSHA256Hash(input, TestHashKey);
            Assert.Equal(expectedHash, hash);
        }
    }
}
