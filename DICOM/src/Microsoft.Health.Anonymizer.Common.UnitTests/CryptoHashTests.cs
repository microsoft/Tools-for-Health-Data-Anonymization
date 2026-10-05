// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Settings;
using Xunit;

namespace Microsoft.Health.Anonymizer.Common.UnitTests
{
    public class CryptoHashTests
    {
        private const string TestHashKey = "123";
        private readonly CryptoHashFunction _function = new CryptoHashFunction(new CryptoHashSetting() { CryptoHashKey = TestHashKey });

        public static IEnumerable<object[]> GetHmacHashStringData()
        {
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

        public static IEnumerable<object[]> GetHmac512HashStringData()
        {
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

        public static IEnumerable<object[]> GetHmac512HashBytesData()
        {
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
            var hashData = CryptoHashFunction.Hash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData);
        }

        [Theory]
        [MemberData(nameof(GetHmac512HashBytesData))]
        public void GivenABytes_WhenComputeHmac512_CorrectHashShouldBeReturned(byte[] input, string expectedHash)
        {
            var hashData = CryptoHashFunction.Hash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmac512HashStreamData))]
        public void GivenAStream_WhenComputeHmac512_CorrectHashShouldBeReturned(Stream input, string expectedHash)
        {
            var hashData = CryptoHashFunction.Hash(input, new HMACSHA512(Encoding.UTF8.GetBytes(TestHashKey)));
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmacHashStringData))]
        public void GivenAString_WhenComputeHmac_CorrectHashShouldBeReturned(string input, string expectedHash)
        {
            var hashData = _function.Hash(input);
            Assert.Equal(expectedHash, hashData);
        }

        [Theory]
        [MemberData(nameof(GetHmacHashBytesData))]

        public void GivenBytes_WhenComputeHmac_CorrectHashShouldBeReturned(byte[] input, string expectedHash)
        {
            var hashData = _function.Hash(input);
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [MemberData(nameof(GetHmacHashStreamData))]

        public void GivenStream_WhenComputeHmac_CorrectHashShouldBeReturned(Stream input, string expectedHash)
        {
            var hashData = _function.Hash(input);
            Assert.Equal(expectedHash, hashData == null ? null : string.Concat(hashData.Select(b => b.ToString("x2"))));
        }

        [Theory]
        [InlineData(19, "f497b5ec6b8aba24571b8788135b8e4f90dd9209ef2928f7a8cb599f2ae46a4e")]
        [InlineData(64, "044fc3b038df1ee644098fb572d3b346a01e985dd85558d35fc0b0d0747a5e00")]
        [InlineData(4096, "a12fe21cf018c5957a661316c9ff73c12a8c211dd7de2336ad94b057b58ea793")]
        public void GivenOverflowBoundaryInput_WhenMatchingLength_OutputIsStableNumericAndDoesNotOverflow(int length, string expectedOutputHash)
        {
            var function = new CryptoHashFunction(new CryptoHashSetting
            {
                CryptoHashKey = TestHashKey,
                MatchInputStringLength = true,
            });
            var input = new string('9', length);

            var first = function.Hash(input);
            var second = function.Hash(input);

            Assert.Equal(input.Length, first.Length);
            Assert.Equal(first, second);
            Assert.All(first, value => Assert.InRange(value, '0', '9'));
            Assert.Equal(expectedOutputHash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first))).ToLowerInvariant());
        }

        [Theory]
        [InlineData(19, "f497b5ec6b8aba24571b8788135b8e4f90dd9209ef2928f7a8cb599f2ae46a4e")]
        [InlineData(64, "044fc3b038df1ee644098fb572d3b346a01e985dd85558d35fc0b0d0747a5e00")]
        [InlineData(4096, "a12fe21cf018c5957a661316c9ff73c12a8c211dd7de2336ad94b057b58ea793")]
        public void GivenStaticMatchLengthHash_WhenHashing_OutputIsStableNumericAndExactLength(int length, string expectedOutputHash)
        {
            var input = new string('9', length);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestHashKey));

            var output = CryptoHashFunction.Hash(input, hmac, matchInputLength: true);

            Assert.Equal(length, output.Length);
            Assert.All(output, value => Assert.InRange(value, '0', '9'));
            Assert.Equal(expectedOutputHash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))).ToLowerInvariant());
        }

        [Fact]
        public async Task GivenConcurrentOperations_WhenHashing_ResultsRemainDeterministic()
        {
            var expected = _function.Hash("concurrent-value");
            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() => _function.Hash("concurrent-value")));

            var results = await Task.WhenAll(tasks);

            Assert.All(results, result => Assert.Equal(expected, result));
        }

        [Fact]
        public void GivenNonPowerOfTwoAlphabet_WhenHashing_OutputIsDeterministicAndConstrained()
        {
            const string alphabet = "0123456789";

            var first = _function.HashToAlphabet("alphabet-input", alphabet, 32);
            var second = _function.HashToAlphabet("alphabet-input", alphabet, 32);

            Assert.Equal("91464668237566287822220487066907", first);
            Assert.Equal(first, second);
            Assert.All(first, value => Assert.Contains(value, alphabet));
        }

        [Theory]
        [InlineData(HashAlgorithmType.Sha256, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData(HashAlgorithmType.Sha256, 1, "7902699be42c8a8e46fbbb4501726517e86b22c56a189f7625a6da49081b2451")]
        [InlineData(HashAlgorithmType.Sha256, 16, "b9858c51db1170748c9e068382c852a68a187fa3def1628b90c1e4231ffb6a28")]
        [InlineData(HashAlgorithmType.Sha256, 64, "044fc3b038df1ee644098fb572d3b346a01e985dd85558d35fc0b0d0747a5e00")]
        [InlineData(HashAlgorithmType.Sha256, 4095, "14f0c4283d47ec0519e8ce2d125a85fbd5af7162b4d4119587d9f942bcd560b4")]
        [InlineData(HashAlgorithmType.Sha256, 4096, "a12fe21cf018c5957a661316c9ff73c12a8c211dd7de2336ad94b057b58ea793")]
        [InlineData(HashAlgorithmType.Sha384, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData(HashAlgorithmType.Sha384, 1, "4e07408562bedb8b60ce05c1decfe3ad16b72230967de01f640b7e4729b49fce")]
        [InlineData(HashAlgorithmType.Sha384, 16, "c075fae3970fcc9e72433fa8d51ec1131377b7007dcbdb7d8f707adf1a87f6bb")]
        [InlineData(HashAlgorithmType.Sha384, 64, "01197d74ecc9126b04c5cd415c537ce92165500851e9710bc26e21204a4fa7b2")]
        [InlineData(HashAlgorithmType.Sha384, 4095, "f67019b18f631248528697c02fe74d7a5c309bd579feb275b7c4b263172a2f4a")]
        [InlineData(HashAlgorithmType.Sha384, 4096, "012df9b0ef83c63dc3fcf96c2bdd81e3ee4d4cf32616729e1906306b0cdc8a0e")]
        [InlineData(HashAlgorithmType.Sha512, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData(HashAlgorithmType.Sha512, 1, "d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35")]
        [InlineData(HashAlgorithmType.Sha512, 16, "247e02ce238e3312213bef6127192e2caf54f1549c5d48adbda862aac8de06ed")]
        [InlineData(HashAlgorithmType.Sha512, 64, "54a250ebcc5e609e9de8cbe999cc4625ae0c230207c537b4f9274de078ed8fec")]
        [InlineData(HashAlgorithmType.Sha512, 4095, "fca969919d78e9480b5f4aa5d303a36b72738d3050d9161d4d107fe5427bfa6e")]
        [InlineData(HashAlgorithmType.Sha512, 4096, "89078446ac450872bc004b16b0fc8370590034000802c44c63be48f942635375")]
        public void GivenBoundedExpansion_WhenMatchingLength_PreviousOutputBytesArePreserved(HashAlgorithmType algorithm, int length, string expectedOutputHash)
        {
            var function = new CryptoHashFunction(new CryptoHashSetting
            {
                CryptoHashKey = TestHashKey,
                CryptoHashType = algorithm,
                MatchInputStringLength = true,
            });
            using var hmac = CreateHmac(algorithm);
            var input = new string('9', length);

            var instanceOutput = function.Hash(input);
            var staticOutput = CryptoHashFunction.Hash(input, hmac, matchInputLength: true);

            Assert.Equal(instanceOutput, staticOutput);
            Assert.Equal(length, instanceOutput.Length);
            Assert.Equal(expectedOutputHash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(instanceOutput))).ToLowerInvariant());
        }

        [Theory]
        [InlineData("instance")]
        [InlineData("static")]
        [InlineData("alphabet")]
        public void GivenOversizedExpansion_WhenHashing_ExplicitValueSafeErrorIsThrown(string mode)
        {
            var input = new string('9', 4097);
            var function = new CryptoHashFunction(new CryptoHashSetting { CryptoHashKey = TestHashKey, MatchInputStringLength = true });
            using var hmac = CreateHmac(HashAlgorithmType.Sha256);
            Action action = mode switch
            {
                "instance" => () => function.Hash(input),
                "static" => () => CryptoHashFunction.Hash(input, hmac, matchInputLength: true),
                _ => () => function.HashToAlphabet(input, "0123456789", 4097),
            };

            var error = Assert.Throws<AnonymizerException>(action);

            Assert.Equal(AnonymizerErrorCode.CryptoHashFailed, error.AnonymizerErrorCode);
            Assert.Equal("Hash output length exceeds the supported limit of 4096 characters.", error.Message);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void GivenOversizedExpansion_WhenGeneratingOutput_NoHashWorkOccurs()
        {
            var calls = 0;
            var invocation = Assert.Throws<TargetInvocationException>(() =>
                InvokeExpansion(Encoding.UTF8.GetBytes("SYNTHETIC"), "0123456789abcdef", 4097, _ =>
                {
                    calls++;
                    return new byte[32];
                }));
            var error = Assert.IsType<AnonymizerException>(invocation.InnerException);

            Assert.Equal(AnonymizerErrorCode.CryptoHashFailed, error.AnonymizerErrorCode);
            Assert.Equal(0, calls);
        }

        [Fact]
        public void GivenLongInputWithFixedOutput_WhenHashing_InputIsNotSubjectToExpansionLimit()
        {
            var input = new string('Z', 16384);
            using var hmac = CreateHmac(HashAlgorithmType.Sha256);
            var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            var expectedHex = Convert.ToHexString(expectedBytes).ToLowerInvariant();

            Assert.Equal(expectedHex, _function.Hash(input));
            Assert.Equal(expectedBytes, _function.Hash(Encoding.UTF8.GetBytes(input)));
            Assert.Equal(expectedBytes, _function.Hash(new MemoryStream(Encoding.UTF8.GetBytes(input))));
            Assert.Equal("750c273ecdb4bc4ab4d14dcae50b38f48f98597415134be8f3dbbc9f67a0c81c", _function.HashToAlphabet(input, "0123456789abcdef", 64));
        }

        [Theory]
        [InlineData(HashAlgorithmType.Sha256)]
        [InlineData(HashAlgorithmType.Sha384)]
        [InlineData(HashAlgorithmType.Sha512)]
        public void GivenLeastAcceptingValidAlphabet_WhenExpanding_MaximumOutputCompletesWithinBudget(HashAlgorithmType algorithm)
        {
            var alphabet = new string(Enumerable.Range(0, 129).Select(value => (char)value).ToArray());
            var minimumAcceptance = Enumerable.Range(1, 256).Min(length => 256 - (256 % length));
            var calls = 0;
            using var hmac = CreateHmac(algorithm);
            Func<byte[], byte[]> hash = bytes =>
            {
                calls++;
                return hmac.ComputeHash(bytes);
            };

            var output = InvokeExpansion(Encoding.UTF8.GetBytes("alphabet-input"), alphabet, 4096, hash);

            Assert.Equal(129, minimumAcceptance);
            Assert.Equal(4096, output.Length);
            Assert.All(output, value => Assert.Contains(value, alphabet));
            Assert.InRange(calls, 1, 1024);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenRejectionSamplingWithoutEnoughProgress_WhenExpanding_BlockBudgetFailsWithoutPartialOutput(bool partialProgress)
        {
            var calls = 0;
            Func<byte[], byte[]> hash = _ =>
            {
                if (++calls > 1024)
                {
                    throw new InvalidOperationException("Controlled test operation budget exceeded.");
                }

                return Enumerable.Repeat(partialProgress && calls == 1 ? (byte)0 : byte.MaxValue, 32).ToArray();
            };

            var invocation = Assert.Throws<TargetInvocationException>(() =>
                InvokeExpansion(Encoding.UTF8.GetBytes("SYNTHETIC-INPUT-SENTINEL"), "0123456789", 64, hash));
            var error = Assert.IsType<AnonymizerException>(invocation.InnerException);

            Assert.Equal(1024, calls);
            Assert.Equal(AnonymizerErrorCode.CryptoHashFailed, error.AnonymizerErrorCode);
            Assert.Equal("Hash output expansion exceeds the supported limit of 1024 HMAC blocks.", error.Message);
            Assert.Null(error.InnerException);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(1025)]
        public void GivenExpansionAtBlockBudgetBoundary_WhenGeneratingOutput_ExactBudgetIsAllowed(int length)
        {
            var calls = 0;
            Func<byte[], byte[]> hash = _ =>
            {
                if (++calls > 1025)
                {
                    throw new InvalidOperationException("Controlled test operation budget exceeded.");
                }

                return new[] { (byte)0 };
            };

            if (length == 1024)
            {
                Assert.Equal(new string('0', length), InvokeExpansion(Array.Empty<byte>(), "0123456789", length, hash));
            }
            else
            {
                var invocation = Assert.Throws<TargetInvocationException>(() =>
                    InvokeExpansion(Array.Empty<byte>(), "0123456789", length, hash));
                var error = Assert.IsType<AnonymizerException>(invocation.InnerException);
                Assert.Equal(AnonymizerErrorCode.CryptoHashFailed, error.AnonymizerErrorCode);
            }

            Assert.Equal(1024, calls);
        }

        [Fact]
        public void GivenExpansionAcrossSeveralBlocks_WhenHashing_OneBufferPreservesInputAndCounterFraming()
        {
            var input = Encoding.UTF8.GetBytes("SYNTHETIC");
            var firstBuffer = Array.Empty<byte>();
            var counters = new List<int>();
            using var hmac = CreateHmac(HashAlgorithmType.Sha256);
            Func<byte[], byte[]> hash = block =>
            {
                if (counters.Count == 0)
                {
                    firstBuffer = block;
                }

                Assert.Same(firstBuffer, block);
                Assert.Equal(input, block.Take(input.Length));
                Assert.Equal(input.Length + sizeof(int), block.Length);
                counters.Add(BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(input.Length)));
                return hmac.ComputeHash(block);
            };

            var output = InvokeExpansion(input, "0123456789abcdef", 128, hash);

            Assert.Equal(new[] { 0, 1, 2, 3 }, counters);
            Assert.Equal(_function.HashToAlphabet("SYNTHETIC", "0123456789abcdef", 128), output);
            Assert.Equal(Encoding.UTF8.GetBytes("SYNTHETIC"), input);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(257)]
        public void GivenInvalidAlphabet_WhenExpanding_NoHashWorkOccurs(int alphabetLength)
        {
            var calls = 0;
            var invocation = Assert.Throws<TargetInvocationException>(() =>
                InvokeExpansion(Array.Empty<byte>(), new string('A', alphabetLength), 1, _ =>
                {
                    calls++;
                    return new byte[32];
                }));

            Assert.IsType<ArgumentOutOfRangeException>(invocation.InnerException);
            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task GivenConcurrentExpansions_WhenHashing_BuffersAndBudgetsArePerOperation()
        {
            var expected = _function.HashToAlphabet("concurrent-value", "0123456789", 4096);
            var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
                _function.HashToAlphabet("concurrent-value", "0123456789", 4096)));

            Assert.All(await Task.WhenAll(tasks), result => Assert.Equal(expected, result));
        }

        private static string InvokeExpansion(byte[] input, string alphabet, int length, Func<byte[], byte[]> hash)
        {
            var method = typeof(CryptoHashFunction).GetMethod("GenerateOutputFromAlphabet", BindingFlags.NonPublic | BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { input, alphabet, length, hash });
        }

        private static HMAC CreateHmac(HashAlgorithmType algorithm)
        {
            var key = Encoding.UTF8.GetBytes(TestHashKey);
            return algorithm switch
            {
                HashAlgorithmType.Sha256 => new HMACSHA256(key),
                HashAlgorithmType.Sha384 => new HMACSHA384(key),
                HashAlgorithmType.Sha512 => new HMACSHA512(key),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
            };
        }
    }
}
