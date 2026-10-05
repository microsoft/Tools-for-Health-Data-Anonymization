// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class DeterministicUidTests
    {
        internal const string Key = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";
        internal const string Expected = "2.25.7525171091336640073516879484382146261";
        internal const string Policy = "{'rules':[{'tag':'PatientName','method':'remove'},{'tag':'StudyInstanceUID','method':'refreshUID'},{'tag':'SeriesInstanceUID','method':'refreshUID'},{'tag':'SOPInstanceUID','method':'refreshUID'},{'tag':'ReferencedSOPInstanceUID','method':'refreshUID'}]}";

        public static IEnumerable<object[]> InvalidUidCases()
        {
            foreach (string mode in new[] { "dataset", "clone", "inplace" })
            {
                foreach (bool validate in new[] { false, true })
                {
                    foreach (string tag in new[] { "StudyInstanceUID", "SeriesInstanceUID", "SOPInstanceUID", "ReferencedSOPInstanceUID" })
                    {
                        foreach (string value in new[] { "1.02.3", "1..2", "1.2.CANARY", "1.2\0.3", "1.2.3 ", " 1.2.3", "1.2.3\0\0", "1.23\0", "1.2.\u00e9", "1.2\n", "1", "1." + new string('2', 63) })
                        {
                            yield return new object[] { mode, validate, tag, value };
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("execution-a", "1.2.840.10008.1.2.3", "2.25.262495083640442560798679107992978262152")]
        [InlineData("execution-a", "2.25.123", Expected)]
        [InlineData("execution-b", "2.25.123", "2.25.271658221757757919814928075637849036060")]
        [InlineData("scope-\u00e9", "2.25.123", "2.25.38126307092024994451362580157445508591")]
        [InlineData("a\0b", "2.25.123", "2.25.45470757715227617522664675826367941400")]
        public void GivenIndependentGoldenVector_WhenMapped_ExactFramingMatches(string scope, string original, string expected)
        {
            Assert.Equal(expected, Map(original, Settings(scope)));
        }

        [Theory]
        [InlineData("00112233445566778899aabbccddeeff", false, "A6F5F3532A2732351DC3EDFE14B33B8B48B4A6E595C06F714E4503D2CCC77314", "2.25.223160867716660009302557963292261972438")]
        [InlineData("ffeeddccbbaa99887766554433221100", false, "148F592C1416FB4EC45C911EB1F55102070C7B93C05A352B130F9147EB9CFA41", "2.25.17092239811699066236212390328592519568")]
        [InlineData("00112233445566778899aabbccddeeff", true, "0352B348F2110235E343F0825C05748BA73FBEDFD96633E68DEFBC9C255CB649", "2.25.290348407356407567787953456304090258502")]
        public void GivenIndependentlyVerifiedCallerDerivedKey_WhenMapped_MatchesEndToEndVector(string scope, bool whitespace, string expectedKey, string expectedUid)
        {
            // This is a synthetic caller integration example, not a library key-derivation API.
            const string syntheticBatchKey = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+Pw==";
            var utf8 = new UTF8Encoding(false, true);
            byte[] inputKey = utf8.GetBytes(whitespace ? " " + syntheticBatchKey + "\n" : syntheticBatchKey);
            byte[] scopeBytes = utf8.GetBytes(scope);
            byte[] prefix = Encoding.ASCII.GetBytes("RefreshUID/batch-key/v1\0");
            byte[] info = new byte[prefix.Length + 4 + scopeBytes.Length];
            prefix.CopyTo(info, 0);
            BinaryPrimitives.WriteUInt32BigEndian(info.AsSpan(prefix.Length, 4), (uint)scopeBytes.Length);
            scopeBytes.CopyTo(info, prefix.Length + 4);
            byte[] key = HKDF.DeriveKey(HashAlgorithmName.SHA256, inputKey, 32, Encoding.ASCII.GetBytes("Microsoft.Health.Dicom.Anonymizer/UID-subkey/v1"), info);
            Assert.Equal(expectedKey, Convert.ToHexString(key));
            var settings = new UidMappingSettings(UidMappingSettings.HmacSha256128V1, Convert.ToBase64String(key), scope);
            CryptographicOperations.ZeroMemory(key);
            Assert.Equal(expectedUid, Map("2.25.123", settings));
        }

        [Fact]
        public void GivenSettings_WhenSerializedOrPrinted_NoSecretOrScopeIsExposed()
        {
            var settings = Settings();
            Assert.Equal("{}", JsonConvert.SerializeObject(settings));
            Assert.Equal("{}", System.Text.Json.JsonSerializer.Serialize(settings));
            Assert.DoesNotContain(Key, settings.ToString());
            Assert.DoesNotContain("execution-a", settings.ToString());
        }

        [Theory]
        [InlineData(null, Key, "execution-a")]
        [InlineData("", Key, "execution-a")]
        [InlineData("HMAC-SHA256-128-V1", Key, "execution-a")]
        [InlineData("future-mode", Key, "execution-a")]
        [InlineData(UidMappingSettings.HmacSha256128V1, null, "execution-a")]
        [InlineData(UidMappingSettings.HmacSha256128V1, "CANARY-invalid-base64", "execution-a")]
        [InlineData(UidMappingSettings.HmacSha256128V1, "AA==", "execution-a")]
        [InlineData(UidMappingSettings.HmacSha256128V1, Key, null)]
        [InlineData(UidMappingSettings.HmacSha256128V1, Key, "")]
        [InlineData(UidMappingSettings.HmacSha256128V1, Key, " \t")]
        public void GivenInvalidSettings_WhenConstructed_FailsWithoutSecretText(string mode, string key, string scope)
        {
            var error = Assert.Throws<ArgumentException>(() => new UidMappingSettings(mode, key, scope));
            Assert.DoesNotContain("CANARY", error.ToString());
            Assert.DoesNotContain(Key, error.ToString());
            Assert.DoesNotContain("execution-a", error.ToString());
            Assert.Null(error.InnerException);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(31)]
        [InlineData(33)]
        [InlineData(64)]
        public void GivenWrongDecodedKeyLength_WhenConstructed_RejectsInsteadOfTruncating(int length)
        {
            Assert.Throws<ArgumentException>(() => new UidMappingSettings(UidMappingSettings.HmacSha256128V1, Convert.ToBase64String(new byte[length]), "execution-a"));
        }

        [Fact]
        public void GivenScopeEncoding_WhenConstructed_StrictUtf8ByteBoundsAndExactIdentityAreEnforced()
        {
            Assert.Throws<ArgumentException>(() => Settings("\ud800"));
            Assert.Throws<ArgumentException>(() => Settings(new string('a', 257)));
            Assert.Throws<ArgumentException>(() => Settings(new string('\u00e9', 129)));
            Assert.NotNull(Settings(new string('\u00e9', 128)));
            Assert.NotNull(Settings(new string('a', 256)));
            Assert.Equal("2.25.120331797074086106972758251940088987717", Map("1." + new string('2', 62), Settings(new string('\u00e9', 128))));
            Assert.NotEqual(Map("2.25.123", Settings("a")), Map("2.25.123", Settings("a ")));
            Assert.NotEqual(Map("2.25.123", Settings("a")), Map("2.25.123", Settings("A")));
            Assert.NotEqual(Map("2.25.123", Settings("\u00e9")), Map("2.25.123", Settings("e\u0301")));
            Assert.NotEqual(Map("2.25.123", Settings("a\0b")), Map("2.25.123", Settings("ab")));
        }

        [Fact]
        public void GivenSharedAndSeparateEngines_WhenParallel_RolesAndRootIdsDoNotSaltMapping()
        {
            var engine = Engine();
            var settings = Keys();
            const string original = "2.25.100000000000000000000123";
            string expected = Map(original, settings.UidMapping);
            Assert.False(RefreshUIDProcessor.ReplacedUIDs.ContainsKey(original));
            Parallel.For(0, 100, index =>
            {
                var file = File("2.25." + (1000 + index));
                var roles = new[] { DicomTag.StudyInstanceUID, DicomTag.SeriesInstanceUID, DicomTag.SOPInstanceUID };
                var role = roles[index % 3];
                file.Dataset.AddOrUpdate(role, original);
                file.Dataset.Add(new DicomSequence(DicomTag.ReferencedImageSequence, new DicomDataset
                {
                    { DicomTag.ReferencedSOPInstanceUID, original },
                    { DicomTag.ReferencedSOPClassUID, DicomUID.CTImageStorage },
                }));
                (index % 2 == 0 ? engine : Engine()).AnonymizeDataset(file.Dataset, settings);
                Assert.Equal(expected, file.Dataset.GetString(role));
                Assert.Equal(expected, file.Dataset.GetSequence(DicomTag.ReferencedImageSequence).Items[0].GetString(DicomTag.ReferencedSOPInstanceUID));
            });
            Assert.False(RefreshUIDProcessor.ReplacedUIDs.ContainsKey(original));
            Assert.NotEqual(expected, Map(original, new UidMappingSettings(UidMappingSettings.HmacSha256128V1, Convert.ToBase64String(new byte[32]), "execution-a")));
        }

        [Fact]
        public void GivenNullMapping_WhenLegacyRuns_SameProcessBatchesShareCacheButOptInNeverUsesIt()
        {
            const string original = "2.25.100000000000000000000124";
            string legacy = Map(original, null);
            Assert.Equal(legacy, Map(original, null));
            string deterministic = Map(original, Settings());
            Assert.NotEqual(legacy, deterministic);
            Assert.Equal(legacy, RefreshUIDProcessor.ReplacedUIDs[original].UID);
            Assert.NotEqual(deterministic, Map(original, Settings("execution-b")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\\")]
        [InlineData("\\2.25.123\\")]
        [InlineData("2.25.123\\\\2.25.123\\2.25.124")]
        public void GivenMultiValueUi_WhenMapped_EmptyPositionsAndMultiplicityArePreserved(string original)
        {
            var tag = DicomTag.FailedSOPInstanceUIDList;
            var dataset = new DicomDataset(Raw(tag, original));
            var before = dataset.GetValues<string>(tag);
            new RefreshUIDProcessor().Process(dataset, dataset.GetDicomItem<DicomItem>(tag), new ProcessContext { RuntimeKeys = Keys() });
            var after = dataset.GetValues<string>(tag);
            Assert.Equal(before.Length, after.Length);
            for (int index = 0; index < before.Length; index++)
            {
                Assert.Equal(before[index].Length == 0 ? string.Empty : Map(before[index], Settings()), after[index]);
            }

            dataset.Validate();
        }

        [Fact]
        public void GivenCanonicalBoundsAndCorrectTerminalNull_WhenMapped_OutputsAreValidAndIdentical()
        {
            Assert.Equal(Map("2.25.1234", Settings()), Map("2.25.1234\0", Settings()));
            foreach (string uid in new[] { "0.0", "1." + new string('2', 62), "2.25.123" })
            {
                string result = Map(uid, Settings());
                Assert.InRange(result.Length, 6, 44);
                Assert.Matches(@"^2\.25\.(0|[1-9][0-9]*)$", result);
                new DicomDataset { { DicomTag.SOPInstanceUID, result } }.Validate();
            }
        }

        [Theory]
        [MemberData(nameof(InvalidUidCases))]
        public void GivenInvalidSelectedUi_WhenPreflighting_FailsSafelyBeforeEarlierMutations(string mode, bool validate, string tagName, string original)
        {
            var file = File();
            DicomUtility.DisableAutoValidation(file.Dataset);
            var tag = Assert.IsType<DicomTag>(typeof(DicomTag).GetField(tagName)?.GetValue(null));
            if (tag == DicomTag.ReferencedSOPInstanceUID)
            {
                var nested = new DicomDataset();
                DicomUtility.DisableAutoValidation(nested);
                nested.Add(Raw(tag, original));
                file.Dataset.Add(new DicomSequence(DicomTag.ReferencedImageSequence, nested));
            }
            else
            {
                file.Dataset.AddOrUpdate(Raw(tag, original));
                if (tag == DicomTag.SOPInstanceUID)
                {
                    DicomUtility.DisableAutoValidation(file.FileMetaInfo);
                    file.FileMetaInfo.AddOrUpdate(Raw(DicomTag.MediaStorageSOPInstanceUID, original));
                }
            }

            byte[] identity = file.Dataset.GetDicomItem<DicomElement>(DicomTag.SOPInstanceUID).Buffer.Data;
            var error = Assert.Throws<AnonymizerOperationException>(() => Apply(Engine(validate), file, mode, Keys()));
            Assert.Equal("Deterministic UID mapping requires a valid UI value.", error.Message);
            Assert.DoesNotContain("CANARY", error.ToString());
            Assert.Equal("SYNTHETIC^NAME", file.Dataset.GetString(DicomTag.PatientName));
            Assert.Equal(identity, file.Dataset.GetDicomItem<DicomElement>(DicomTag.SOPInstanceUID).Buffer.Data);
        }

        [Theory]
        [InlineData("SOPClassUID")]
        [InlineData("ReferencedSOPClassUID")]
        [InlineData("MediaStorageSOPClassUID")]
        [InlineData("TransferSyntaxUID")]
        [InlineData("SOPClassesInStudy")]
        public void GivenInvariantUid_WhenRefreshSelected_PreflightRejects(string tagName)
        {
            var tag = Assert.IsType<DicomTag>(typeof(DicomTag).GetField(tagName)?.GetValue(null));
            var dataset = new DicomDataset { { tag, "1.2.840.10008.1.2" } };
            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(
                "{'rules':[{'tag':'" + tagName + "','method':'refreshUID'}]}"));
            Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset, Keys()));
            Assert.Equal("1.2.840.10008.1.2", dataset.GetString(tag));
        }

        [Theory]
        [InlineData("keep")]
        [InlineData("unselected")]
        [InlineData("remove")]
        [InlineData("redact")]
        public void GivenReferencePolicyDisposition_WhenMapping_NoPreservationOrRepairIsInvented(string disposition)
        {
            var file = File();
            var nested = new DicomDataset { { DicomTag.ReferencedSOPInstanceUID, "2.25.123" } };
            file.Dataset.Add(new DicomSequence(DicomTag.ReferencedImageSequence, nested));
            string first = disposition == "unselected" ? string.Empty :
                "{'tag':'" + (disposition == "keep" ? "ReferencedSOPInstanceUID" : "ReferencedImageSequence") + "','method':'" + disposition + "','params':{}},";
            string last = disposition == "unselected" ? string.Empty : ",{'tag':'ReferencedSOPInstanceUID','method':'refreshUID'}";
            if (disposition == "remove" || disposition == "redact")
            {
                DicomUtility.DisableAutoValidation(nested);
                nested.AddOrUpdate(Raw(DicomTag.ReferencedSOPInstanceUID, "INVALID.CANARY"));
            }

            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(
                "{'rules':[" + first + "{'tag':'SOPInstanceUID','method':'refreshUID'}" + last + "]}"));
            engine.AnonymizeFileInPlace(file, Keys());
            Assert.Equal(Expected, file.Dataset.GetString(DicomTag.SOPInstanceUID));
            if (disposition == "remove")
            {
                Assert.False(file.Dataset.Contains(DicomTag.ReferencedImageSequence));
            }
            else if (disposition == "redact")
            {
                Assert.Empty(file.Dataset.GetSequence(DicomTag.ReferencedImageSequence).Items);
            }
            else
            {
                Assert.Equal("2.25.123", nested.GetString(DicomTag.ReferencedSOPInstanceUID));
                Assert.NotEqual(file.Dataset.GetString(DicomTag.SOPInstanceUID), nested.GetString(DicomTag.ReferencedSOPInstanceUID));
            }
        }

        [Theory]
        [InlineData("dataset", false)]
        [InlineData("clone", false)]
        [InlineData("inplace", false)]
        [InlineData("dataset", true)]
        [InlineData("clone", true)]
        [InlineData("inplace", true)]
        public void GivenCallerSwapsMappingDuringProcessing_EntryPointSnapshotIncludingNullIsPreserved(string mode, bool initiallyNull)
        {
            var keys = initiallyNull ? new RuntimeKeySettings() : Keys();
            var factory = new SwitchingFactory(() => keys.UidMapping = Settings("execution-b"));
            var config = AnonymizerConfigurationManager.CreateFromJson(Policy);
            var file = File();
            file.Dataset.Add(new DicomSequence(DicomTag.ReferencedImageSequence, new DicomDataset { { DicomTag.ReferencedSOPInstanceUID, "2.25.123" } }));
            string expected = initiallyNull ? Map("2.25.123", null) : Expected;
            var output = Apply(new AnonymizerEngine(config, processorFactory: factory), file, mode, keys);
            Assert.Equal(expected, output.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal(expected, output.Dataset.GetSequence(DicomTag.ReferencedImageSequence).Items[0].GetString(DicomTag.ReferencedSOPInstanceUID));
            Assert.NotEqual(expected, Map("2.25.123", keys.UidMapping));
        }

        internal static UidMappingSettings Settings(string scope = "execution-a") => new UidMappingSettings(UidMappingSettings.HmacSha256128V1, Key, scope);

        internal static RuntimeKeySettings Keys(string scope = "execution-a") => new RuntimeKeySettings { UidMapping = Settings(scope) };

        internal static AnonymizerEngine Engine(bool validate = false) => new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(Policy), new AnonymizerEngineOptions(validate, validate));

        internal static DicomFile File(string instance = "2.25.123") => new DicomFile(new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
            { DicomTag.SOPInstanceUID, instance },
            { DicomTag.StudyInstanceUID, "2.25.100" },
            { DicomTag.SeriesInstanceUID, "2.25.101" },
            { DicomTag.PatientName, "SYNTHETIC^NAME" },
            new DicomOtherByte(DicomTag.PixelData, new byte[] { 1, 2, 3, 4 }),
        });

        private static DicomUniqueIdentifier Raw(DicomTag tag, string value) => new DicomUniqueIdentifier(tag, new MemoryByteBuffer(Encoding.UTF8.GetBytes(value)));

        private static string Map(string original, UidMappingSettings? settings)
        {
            var dataset = new DicomDataset();
            DicomUtility.DisableAutoValidation(dataset);
            dataset.Add(Raw(DicomTag.SOPInstanceUID, original));
            new RefreshUIDProcessor().Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.SOPInstanceUID), new ProcessContext { RuntimeKeys = new RuntimeKeySettings { UidMapping = settings } });
            return dataset.GetString(DicomTag.SOPInstanceUID);
        }

        private static DicomFile Apply(AnonymizerEngine engine, DicomFile file, string mode, RuntimeKeySettings keys)
        {
            if (mode == "clone")
            {
                return engine.AnonymizeFile(file, keys);
            }

            if (mode == "inplace")
            {
                engine.AnonymizeFileInPlace(file, keys);
            }
            else
            {
                engine.AnonymizeDataset(file.Dataset, keys);
            }

            return file;
        }

        private sealed class SwitchingFactory : IAnonymizerProcessorFactory
        {
            private readonly Action _action;

            public SwitchingFactory(Action action) => _action = action;

            public IAnonymizerProcessor CreateProcessor(string anonymizeMethod, JObject? ruleSetting = null) =>
                anonymizeMethod.Equals("remove", StringComparison.OrdinalIgnoreCase) ? new SwitchingProcessor(_action) : new DicomProcessorFactory().CreateProcessor(anonymizeMethod, ruleSetting ?? new JObject());
        }

        private sealed class SwitchingProcessor : IAnonymizerProcessor
        {
            private readonly Action _action;

            public SwitchingProcessor(Action action) => _action = action;

            public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext? context = null) => _action();

            public bool IsSupported(DicomItem item) => true;
        }
    }
}
