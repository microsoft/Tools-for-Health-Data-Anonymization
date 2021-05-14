// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Microsoft.Health.Dicom.DeID.SharedLib.Model;
using Xunit;

namespace De.ID.Function.Shared.UnitTests
{
    public class CryptoHashTests
    {
        private const string TestHashKey = "123";

        public static IEnumerable<object[]> GetHmacHashStringData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, "6d6cd63284be4a47ba7aec4a3458939a95dcbdd5cd0438f23d7457099b4b917c" };
            yield return new object[] { "abc", "8f16771f9f8851b26f4d460fa17de93e2711c7e51337cb8a608a0f81e1c1b6ae" };
            yield return new object[] { "&*^%$@()=-,/", "33f6f7d6b3602bf5354dcb4b8d988982602349355f50f86798d8ce1ffd61521b" };
            yield return new object[] { "ÆŊŋßſ♫∅", "1a94823f0a0f00a4b1ca771c3446dc5e17958f4dae3588ace2bca8a843eb63d9" };
            yield return new object[]
            {
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-",
                "352b5a4af5adb81fa616c2a5b5c492d0b0b544c188a9aa003767a2b5efbd1478",
            };
        }

        public static IEnumerable<object[]> GetHmacHashBytesData()
        {
            yield return new object[] { null, null };
            yield return new object[] { Encoding.UTF8.GetBytes(string.Empty), "6d6cd63284be4a47ba7aec4a3458939a95dcbdd5cd0438f23d7457099b4b917c" };
            yield return new object[] { Encoding.UTF8.GetBytes("abc"), "8f16771f9f8851b26f4d460fa17de93e2711c7e51337cb8a608a0f81e1c1b6ae" };
            yield return new object[] { Encoding.UTF8.GetBytes("&*^%$@()=-,/"), "33f6f7d6b3602bf5354dcb4b8d988982602349355f50f86798d8ce1ffd61521b" };
            yield return new object[] { Encoding.UTF8.GetBytes("ÆŊŋßſ♫∅"), "1a94823f0a0f00a4b1ca771c3446dc5e17958f4dae3588ace2bca8a843eb63d9" };
            yield return new object[]
            {
                Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-"),
                "352b5a4af5adb81fa616c2a5b5c492d0b0b544c188a9aa003767a2b5efbd1478",
            };
        }

        public static IEnumerable<object[]> GetHmacHashStreamData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new MemoryStream(), "6d6cd63284be4a47ba7aec4a3458939a95dcbdd5cd0438f23d7457099b4b917c" };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("abc")), "8f16771f9f8851b26f4d460fa17de93e2711c7e51337cb8a608a0f81e1c1b6ae" };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("&*^%$@()=-,/")), "33f6f7d6b3602bf5354dcb4b8d988982602349355f50f86798d8ce1ffd61521b" };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("ÆŊŋßſ♫∅")), "1a94823f0a0f00a4b1ca771c3446dc5e17958f4dae3588ace2bca8a843eb63d9" };
            yield return new object[]
            {
                new MemoryStream(Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-")),
                "352b5a4af5adb81fa616c2a5b5c492d0b0b544c188a9aa003767a2b5efbd1478",
            };
        }

        public static IEnumerable<object[]> GetHmacHashFixedLengthStringData()
        {
            yield return new object[] { new FixedLengthString(string.Empty), string.Empty };
            yield return new object[] { new FixedLengthString("abc"), "8f1" };
            yield return new object[] { new FixedLengthString("&*^%$@()=-,/"), "33f6f7d6b360" };
            yield return new object[] { new FixedLengthString("ÆŊŋßſ♫∅"), "1a94823" };
            yield return new object[]
            {
                new FixedLengthString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-"),
                "352b5a4af5adb81fa616c2a5b5c492d0b0b544c188a9aa003767a2b5efbd14780000000000",
            };
        }

        public static IEnumerable<object[]> GetHmac512HashStringData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, "79a898c707f0d60e2dc22f96854c1999540f4cdfce6463f74016aa18a3d1003628d47c4e745536afabbdb90d086fad14dadf8b4927cdf55d48b4078a1e9e4525" };
            yield return new object[] { "abc", "58585acd673067f96bea32a1c57bf3fc3fd5a42678567e72d5cb0ab7f08ea41dcf3a41af96c53948e13184ae6fe6cd0b8b4193fc593dfb2693b00c2b0ee7a316" };
            yield return new object[] { "&*^%$@()=-,/", "825483251c4ab2d89e6b8c1ec2e3b770cb805f7d044e6777b2c85d6ffab0c0ab2e14ceaac0291b105c131da7a3add580e07ea977f74652c4bc1d45b4d0fec3e6" };
            yield return new object[] { "ÆŊŋßſ♫∅", "5a8c7f1651833e1d888c69b7478149213ee5945005a46b017fa4f4989cb6311dc0017f7392451c04bd33c16327dd874035111f1580dba17944ece3b11343d395" };
            yield return new object[]
            {
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-",
                "11e46254067ed6e334b1b9ea95872a62526743f6a777131cd66e2373ad43c220fc8674b087f1e6038de0f648ed9e987109f2be38cf5c60b7820f7ae7b7fedcde",
            };
        }

        public static IEnumerable<object[]> GetHmac512HashFixedLengthStringData()
        {
            yield return new object[] { new FixedLengthString(string.Empty), string.Empty };

            yield return new object[] { new FixedLengthString("abc"), "585" };
            yield return new object[] { new FixedLengthString("&*^%$@()=-,/"), "825483251c4a" };
            yield return new object[] { new FixedLengthString("ÆŊŋßſ♫∅"), "5a8c7f1" };
            yield return new object[]
            {
                new FixedLengthString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-"),
                "11e46254067ed6e334b1b9ea95872a62526743f6a777131cd66e2373ad43c220fc8674b087",
            };
        }

        public static IEnumerable<object[]> GetHmac512HashBytesData()
        {
            yield return new object[] { null, null };
            yield return new object[] { Encoding.UTF8.GetBytes(string.Empty), "79a898c707f0d60e2dc22f96854c1999540f4cdfce6463f74016aa18a3d1003628d47c4e745536afabbdb90d086fad14dadf8b4927cdf55d48b4078a1e9e4525" };
            yield return new object[] { Encoding.UTF8.GetBytes("abc"), "58585acd673067f96bea32a1c57bf3fc3fd5a42678567e72d5cb0ab7f08ea41dcf3a41af96c53948e13184ae6fe6cd0b8b4193fc593dfb2693b00c2b0ee7a316" };
            yield return new object[] { Encoding.UTF8.GetBytes("&*^%$@()=-,/"), "825483251c4ab2d89e6b8c1ec2e3b770cb805f7d044e6777b2c85d6ffab0c0ab2e14ceaac0291b105c131da7a3add580e07ea977f74652c4bc1d45b4d0fec3e6" };
            yield return new object[] { Encoding.UTF8.GetBytes("ÆŊŋßſ♫∅"), "5a8c7f1651833e1d888c69b7478149213ee5945005a46b017fa4f4989cb6311dc0017f7392451c04bd33c16327dd874035111f1580dba17944ece3b11343d395" };
            yield return new object[]
            {
                Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-"),
                "11e46254067ed6e334b1b9ea95872a62526743f6a777131cd66e2373ad43c220fc8674b087f1e6038de0f648ed9e987109f2be38cf5c60b7820f7ae7b7fedcde",
            };
        }

        public static IEnumerable<object[]> GetHmac512HashStreamData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new MemoryStream(), "79a898c707f0d60e2dc22f96854c1999540f4cdfce6463f74016aa18a3d1003628d47c4e745536afabbdb90d086fad14dadf8b4927cdf55d48b4078a1e9e4525" };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("abc")), "58585acd673067f96bea32a1c57bf3fc3fd5a42678567e72d5cb0ab7f08ea41dcf3a41af96c53948e13184ae6fe6cd0b8b4193fc593dfb2693b00c2b0ee7a316" };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("&*^%$@()=-,/")), "825483251c4ab2d89e6b8c1ec2e3b770cb805f7d044e6777b2c85d6ffab0c0ab2e14ceaac0291b105c131da7a3add580e07ea977f74652c4bc1d45b4d0fec3e6" };
            yield return new object[] { new MemoryStream(Encoding.UTF8.GetBytes("ÆŊŋßſ♫∅")), "5a8c7f1651833e1d888c69b7478149213ee5945005a46b017fa4f4989cb6311dc0017f7392451c04bd33c16327dd874035111f1580dba17944ece3b11343d395" };
            yield return new object[]
            {
                new MemoryStream(Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()=-")),
                "11e46254067ed6e334b1b9ea95872a62526743f6a777131cd66e2373ad43c220fc8674b087f1e6038de0f648ed9e987109f2be38cf5c60b7820f7ae7b7fedcde",
            };
        }

        [Theory]
        [MemberData(nameof(GetHmac512HashStringData))]
        public void GivenAString_WhenComputeHmac512_CorrectHashShouldBeReturned(string input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacHash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmac512HashBytesData))]
        public void GivenABytes_WhenComputeHmac512_CorrectHashShouldBeReturned(byte[] input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacHash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmac512HashStreamData))]
        public void GivenAStream_WhenComputeHmac512_CorrectHashShouldBeReturned(Stream input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacHash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }


        [Theory]
        [MemberData(nameof(GetHmac512HashFixedLengthStringData))]

        public void GivenFixedLengthString_WhenComputeHmac512_CorrectHashShouldBeReturned(FixedLengthString input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacHash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData.ToString());
        }

        [Theory]
        [MemberData(nameof(GetHmacHashStringData))]
        public void GivenAString_WhenComputeHmac_CorrectHashShouldBeReturned(string input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacSHA256Hash(input, Encoding.UTF8.GetBytes(TestHashKey));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmacHashBytesData))]

        public void GivenBytes_WhenComputeHmac_CorrectHashShouldBeReturned(byte[] input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacSHA256Hash(input, Encoding.UTF8.GetBytes(TestHashKey));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmacHashStreamData))]

        public void GivenStream_WhenComputeHmac_CorrectHashShouldBeReturned(Stream input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacSHA256Hash(input, Encoding.UTF8.GetBytes(TestHashKey));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmacHashFixedLengthStringData))]

        public void GivenFixedLengthString_WhenComputeHmac_CorrectHashShouldBeReturned(FixedLengthString input, string expectedHash)
        {
            var hashData = CryptoHashFunction.ComputeHmacSHA256Hash(input, Encoding.UTF8.GetBytes(TestHashKey));
            Assert.Equal(expectedHash, hashData.ToString());
        }
    }
}
