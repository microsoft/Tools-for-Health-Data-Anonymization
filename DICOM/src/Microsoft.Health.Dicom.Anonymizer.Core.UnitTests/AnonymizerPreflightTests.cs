// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerPreflightTests
    {
        [Theory]
        [InlineData("0062,0002", "0062,0005")]
        [InlineData("0040,B020", "0070,0006")]
        [InlineData("0040,0560", "0040,0551")]
        public void GivenExactNestedSubstitution_WhenAnonymizing_NonemptyValueAndStructureArePreserved(string sequenceSelector, string selector)
        {
            var sequenceTag = DicomTag.Parse(sequenceSelector);
            var tag = DicomTag.Parse(selector);
            var nested = new DicomDataset
            {
                { tag, "SYNTHETIC IDENTIFIER" },
                { DicomTag.Manufacturer, "UNCHANGED" },
            };
            var source = CreateFile(new DicomSequence(sequenceTag, nested));
            var vr = nested.GetDicomItem<DicomItem>(tag).ValueRepresentation;
            var engine = CreateEngine(Rule(selector, "substitute", new JObject { ["replaceWith"] = "ANONYMOUS" }));

            var output = engine.AnonymizeFile(source);

            var result = Assert.Single(output.Dataset.GetSequence(sequenceTag).Items);
            Assert.Equal("ANONYMOUS", result.GetString(tag));
            Assert.Equal(vr, result.GetDicomItem<DicomItem>(tag).ValueRepresentation);
            Assert.Equal(1, result.GetDicomItem<DicomElement>(tag).Count);
            Assert.Equal("UNCHANGED", result.GetString(DicomTag.Manufacturer));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, output.Dataset.GetValues<byte>(DicomTag.PixelData));
            Assert.Equal("SYNTHETIC IDENTIFIER", nested.GetString(tag));
            result.Validate();
        }

        [Fact]
        public void GivenNestedStringHash_WhenAnonymizing_EachValueUsesRuntimeKeyAndRetainsMultiplicity()
        {
            var first = new DicomDataset { { DicomTag.ConsultingPhysicianName, "Synthetic^One", "Synthetic^Two" } };
            var second = new DicomDataset { { DicomTag.ConsultingPhysicianName, "Synthetic^One", "Synthetic^Two" } };
            var dataset = new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, first, second) };
            var engine = CreateEngine(Rule("ConsultingPhysicianName", "cryptoHash"));
            var keys = new RuntimeKeySettings { CryptoHashKey = "runtime-synthetic-key" };
            var scalar = new DicomDataset { { DicomTag.ConsultingPhysicianName, "Synthetic^One", "Synthetic^Two" } };

            engine.AnonymizeDataset(dataset, keys);
            engine.AnonymizeDataset(scalar, keys);

            var expected = scalar.GetValues<string>(DicomTag.ConsultingPhysicianName);
            Assert.Equal(2, expected.Length);
            Assert.All(expected, value => Assert.Matches("^[0-9a-f]{64}$", value));
            Assert.Equal(expected, first.GetValues<string>(DicomTag.ConsultingPhysicianName));
            Assert.Equal(expected, second.GetValues<string>(DicomTag.ConsultingPhysicianName));
            first.Validate();
        }

        [Theory]
        [InlineData("PatientID", "remove")]
        [InlineData("PatientID", "redact")]
        [InlineData("Rows", "remove")]
        [InlineData("Rows", "redact")]
        public void GivenExactNestedScalarAction_WhenAnonymizing_DeclaredRemovalOrClearingIsApplied(string selector, string method)
        {
            var tag = Assert.IsType<DicomTag>(typeof(DicomTag).GetField(selector)?.GetValue(null));
            var nested = new DicomDataset { { tag, "12" } };
            var sequence = new DicomSequence(DicomTag.RequestAttributesSequence, nested);
            var dataset = new DicomDataset { sequence };
            var engine = CreateEngine(Rule(selector, method));

            engine.AnonymizeDataset(dataset);

            Assert.Same(sequence, dataset.GetSequence(DicomTag.RequestAttributesSequence));
            Assert.Same(nested, Assert.Single(sequence.Items));
            if (method == "remove")
            {
                Assert.False(nested.Contains(tag));
            }
            else
            {
                Assert.Equal(0, nested.GetDicomItem<DicomElement>(tag).Count);
                nested.Validate();
            }
        }

        [Fact]
        public void GivenNestedPartialDateRedaction_WhenAnonymizing_ExistingSettingsAreApplied()
        {
            var nested = new DicomDataset { { DicomTag.StudyDate, "20250509" } };
            var engine = CreateEngine(Rule("StudyDate", "redact", new JObject { ["enablePartialDatesForRedact"] = true }));

            engine.AnonymizeDataset(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, nested) });

            Assert.Equal("20250101", nested.GetString(DicomTag.StudyDate));
            nested.Validate();
        }

        [Theory]
        [InlineData("remove", false)]
        [InlineData("redact", false)]
        [InlineData("remove", true)]
        [InlineData("redact", true)]
        public void GivenExactSequenceDisposition_WhenAnonymizing_UnsupportedDescendantsAreNotProcessed(string method, bool nestedSequence)
        {
            var child = new DicomDataset { { DicomTag.StudyDate, "20250509" } };
            var parent = new DicomDataset { new DicomSequence(DicomTag.ContentSequence, child) };
            var dataset = nestedSequence
                ? new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, parent) }
                : parent;
            var engine = CreateEngine(
                Rule("ContentSequence", method),
                Rule("StudyDate", "dateShift"),
                Rule("SQ", "keep"));

            engine.AnonymizeDataset(dataset);

            if (method == "remove")
            {
                Assert.False(parent.Contains(DicomTag.ContentSequence));
            }
            else
            {
                Assert.Empty(parent.GetSequence(DicomTag.ContentSequence).Items);
            }

            Assert.Equal("20250509", child.GetString(DicomTag.StudyDate));
        }

        [Theory]
        [InlineData("LO")]
        [InlineData("(0010,xxxx)")]
        public void GivenEarlierLeafKeep_WhenAnonymizing_NestedExactRuleDoesNotOverrideIt(string selector)
        {
            var nested = new DicomDataset { { DicomTag.PatientID, "UNCHANGED" } };
            var engine = CreateEngine(
                Rule(selector, "keep"),
                Rule("PatientID", "substitute", new JObject { ["replaceWith"] = "ANONYMOUS" }));

            engine.AnonymizeDataset(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, nested) });

            Assert.Equal("UNCHANGED", nested.GetString(DicomTag.PatientID));
        }

        [Fact]
        public void GivenSequenceKeepAndExactLeafAction_WhenAnonymizing_LeafActionStillRuns()
        {
            var nested = new DicomDataset { { DicomTag.PatientID, "ORIGINAL" } };
            var engine = CreateEngine(Rule("SQ", "keep"), Rule("PatientID", "cryptoHash"));

            engine.AnonymizeDataset(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, nested) });

            Assert.NotEqual("ORIGINAL", nested.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("LO", "substitute")]
        [InlineData("(0010,xxxx)", "remove")]
        public void GivenOnlyBroadNestedRule_WhenAnonymizing_LegacyRootScopeIsPreserved(string selector, string method)
        {
            var nested = new DicomDataset { { DicomTag.PatientID, "UNCHANGED" } };
            var engine = CreateEngine(Rule(selector, method, new JObject { ["replaceWith"] = "ANONYMOUS" }));

            engine.AnonymizeDataset(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, nested) });

            Assert.Equal("UNCHANGED", nested.GetString(DicomTag.PatientID));
        }

        [Fact]
        public void GivenEarlierBroadTransformForExactNestedCandidate_WhenAnonymizing_PreflightRejectsBeforeMutation()
        {
            var nested = new DicomDataset { { DicomTag.PatientID, "NESTED" } };
            var dataset = new DicomDataset
            {
                { DicomTag.PatientName, "Root^Person" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nested),
            };
            var engine = CreateEngine(
                Rule("PatientName", "cryptoHash"),
                Rule("LO", "redact"),
                Rule("PatientID", "cryptoHash"));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Equal("Root^Person", dataset.GetString(DicomTag.PatientName));
            Assert.Equal("NESTED", nested.GetString(DicomTag.PatientID));
        }

        [Fact]
        public void GivenExactNestedDateShift_WhenAnonymizing_PreflightRejectsWithoutApplyingRootRules()
        {
            var nested = new DicomDataset { { DicomTag.StudyDate, "20250509" } };
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "ROOT IDENTIFIER" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nested),
            };
            var engine = CreateEngine(Rule("PatientID", "cryptoHash"), Rule("StudyDate", "dateShift"));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Contains("(0008,0020)", error.Message);
            Assert.Contains("DA", error.Message);
            Assert.DoesNotContain("20250509", error.ToString());
            Assert.Equal("ROOT IDENTIFIER", dataset.GetString(DicomTag.PatientID));
            Assert.Equal("20250509", nested.GetString(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenNestedCustomImplementationOfBuiltInMethod_WhenAnonymizing_PreflightDoesNotInvokeIt()
        {
            var processor = new TrackingProcessor();
            var nested = new DicomDataset { { DicomTag.PatientID, "UNCHANGED" } };
            var engine = CreateEngine(
                new AnonymizerEngineOptions(),
                new ReplacingHashFactory(processor),
                Rule("PatientID", "cryptoHash"));

            Assert.Throws<AnonymizerOperationException>(() =>
                engine.AnonymizeDataset(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, nested) }));

            Assert.Equal(0, processor.Calls);
            Assert.Equal("UNCHANGED", nested.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("FIRST\\SECOND")]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLM")]
        public void GivenInvalidNestedReplacement_WhenAnonymizing_NoRootOrNestedValueIsChanged(string replacement)
        {
            var nested = new DicomDataset { { DicomTag.PatientID, "NESTED" } };
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            file.Dataset.Add(DicomTag.PatientName, "Root^Person");
            var engine = CreateEngine(
                Rule("PatientName", "cryptoHash"),
                Rule("PatientID", "substitute", new JObject { ["replaceWith"] = replacement }));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFileInPlace(file));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Equal("Root^Person", file.Dataset.GetString(DicomTag.PatientName));
            Assert.Equal("NESTED", nested.GetString(DicomTag.PatientID));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
        }

        [Theory]
        [InlineData("PatientSex", "F", "lowercase")]
        [InlineData("InstanceCreatorUID", "2.25.100", "NOT-A-UID")]
        [InlineData("PatientAge", "010Y", "NOT-AN-AGE")]
        public void GivenNestedReplacementWithInvalidVrAlphabet_WhenAnonymizing_PreflightRejectsSafely(string selector, string original, string replacement)
        {
            var tag = Assert.IsType<DicomTag>(typeof(DicomTag).GetField(selector)?.GetValue(null));
            var nested = new DicomDataset { { tag, original } };
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "ROOT" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nested),
            };
            var engine = CreateEngine(
                Rule("PatientID", "cryptoHash"),
                Rule(selector, "substitute", new JObject { ["replaceWith"] = replacement }));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.DoesNotContain(replacement, error.ToString());
            Assert.Equal("ROOT", dataset.GetString(DicomTag.PatientID));
            Assert.Equal(original, nested.GetString(tag));
        }

        [Fact]
        public void GivenMultiValueNestedSubstitute_WhenAnonymizing_MultiplicityIsNotSilentlyCollapsed()
        {
            var nested = new DicomDataset { { DicomTag.ConsultingPhysicianName, "First^Person", "Second^Person" } };
            var engine = CreateEngine(Rule("ConsultingPhysicianName", "substitute", new JObject { ["replaceWith"] = "ANONYMOUS" }));

            Assert.Throws<AnonymizerOperationException>(() =>
                engine.AnonymizeDataset(new DicomDataset { new DicomSequence(DicomTag.RequestAttributesSequence, nested) }));

            Assert.Equal(new[] { "First^Person", "Second^Person" }, nested.GetValues<string>(DicomTag.ConsultingPhysicianName));
        }

        [Fact]
        public void GivenNestedBulkRemoval_WhenAnonymizing_UnsupportedShapeIsRejectedWithoutReadingIt()
        {
            var buffer = new UnreadableBuffer();
            var nested = new DicomDataset(new DicomOtherByte(DicomTag.PixelData, buffer));
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            var engine = CreateEngine(Rule("PixelData", "remove"));

            Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFileInPlace(file));

            Assert.Equal(0, buffer.ReadAttempts);
            Assert.True(file.Dataset.Contains(DicomTag.PixelData));
            Assert.Same(buffer, nested.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer);
        }

        [Fact]
        public void GivenLaterUnsupportedRootMethod_WhenAnonymizing_EarlierRootRuleIsNotApplied()
        {
            var dataset = new DicomDataset { { DicomTag.PatientName, "Root^Person" }, { DicomTag.PatientID, "IDENTIFIER" } };
            var engine = CreateEngine(Rule("PatientName", "cryptoHash"), Rule("PatientID", "refreshUID"));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Equal("Root^Person", dataset.GetString(DicomTag.PatientName));
            Assert.Equal("IDENTIFIER", dataset.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenKeptLazyBuffers_WhenAnonymizingInPlace_NoBulkDataIsReadOrReplaced(bool validate)
        {
            var rootBuffer = new UnreadableBuffer();
            var nestedBuffer = new UnreadableBuffer();
            var nested = new DicomDataset(
                new DicomOtherByte(DicomTag.PixelData, nestedBuffer),
                new DicomLongString(DicomTag.PatientID, "NESTED"));
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            file.Dataset.AddOrUpdate(new DicomOtherByte(DicomTag.PixelData, rootBuffer));
            var originalMeta = file.FileMetaInfo;
            var originalDataset = file.Dataset;
            var engine = CreateEngine(
                new AnonymizerEngineOptions(validate, validate),
                null,
                Rule("PixelData", "keep"),
                Rule("SOPInstanceUID", "refreshUID"),
                Rule("PatientID", "cryptoHash"));

            engine.AnonymizeFileInPlace(file);

            Assert.Same(originalDataset, file.Dataset);
            Assert.Same(originalMeta, file.FileMetaInfo);
            Assert.Same(rootBuffer, file.Dataset.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer);
            Assert.Same(nestedBuffer, nested.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer);
            Assert.Equal(0, rootBuffer.ReadAttempts);
            Assert.Equal(0, nestedBuffer.ReadAttempts);
            Assert.NotEqual("NESTED", nested.GetString(DicomTag.PatientID));
            Assert.Equal(file.Dataset.GetString(DicomTag.SOPInstanceUID), file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
        }

        [Fact]
        public void GivenStreamBackedPixels_WhenAnonymizingInPlace_SourceStreamRemainsUsableThroughSave()
        {
            var expected = Enumerable.Range(0, 65538).Select(index => (byte)(index % 251)).ToArray();
            using var source = new MemoryStream(expected);
            var buffer = new StreamByteBuffer(source, 0, source.Length);
            var file = CreateFile();
            file.Dataset.AddOrUpdate(new DicomOtherByte(DicomTag.PixelData, buffer));
            var syntax = file.FileMetaInfo.TransferSyntax;

            CreateEngine(Rule("SOPInstanceUID", "refreshUID"), Rule("PixelData", "keep")).AnonymizeFileInPlace(file);

            Assert.Same(buffer, file.Dataset.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer);
            Assert.True(source.CanRead);
            Assert.Equal(syntax, file.FileMetaInfo.TransferSyntax);
            using var saved = new MemoryStream();
            file.Save(saved);
            Assert.True(source.CanRead);
            saved.Position = 0;
            Assert.Equal(expected, DicomFile.Open(saved, FileReadOption.ReadAll).Dataset.GetValues<byte>(DicomTag.PixelData));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenInPlacePreflightFailure_WhenAnonymizing_FileAndKeptBufferAreUnchanged(bool embedded)
        {
            var buffer = new UnreadableBuffer();
            var file = CreateFile();
            file.Dataset.AddOrUpdate(new DicomOtherByte(DicomTag.PixelData, buffer));
            file.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUID.Parse("2.25.999");
            if (embedded)
            {
                file.Dataset.Add(new DicomSequence(DicomTag.ContentSequence, new DicomDataset(new DicomOtherByte(DicomTag.EncapsulatedDocument, new byte[] { 1, 2 }))));
            }

            var error = Assert.Throws<AnonymizerOperationException>(() =>
                CreateEngine(Rule("SOPInstanceUID", "refreshUID")).AnonymizeFileInPlace(file));

            Assert.Equal(embedded ? DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload : DicomAnonymizationErrorCode.FileMetaIdentityMismatch, error.DicomAnonymizerErrorCode);
            Assert.Equal("2.25.100", file.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.999", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(0, buffer.ReadAttempts);
        }

        [Fact]
        public void GivenLateInPlaceOutputFailure_WhenAnonymizing_CallerMustDiscardModifiedFile()
        {
            var file = CreateFile(new DicomLongString(DicomTag.PatientID, "ORIGINAL"));
            var replacement = new string('A', 65);
            var engine = CreateEngine(
                new AnonymizerEngineOptions(false, true),
                null,
                Rule("PatientID", "substitute", new JObject { ["replaceWith"] = replacement }));

            Assert.Throws<DicomValidationException>(() => engine.AnonymizeFileInPlace(file));

            Assert.Equal(replacement, file.Dataset.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("ReferencedSOPClassUID")]
        [InlineData("SOPClassesInStudy")]
        [InlineData("(0008,1150)")]
        [InlineData("00080062")]
        [InlineData("(0008,11xx)")]
        public void GivenClassUidSelector_WhenConstructingEngine_TransformationIsRejected(string selector)
        {
            var error = Assert.Throws<AnonymizerConfigurationException>(() => CreateEngine(Rule(selector, "refreshUID")));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidConfigurationValues, error.DicomAnonymizerErrorCode);
        }

        [Theory]
        [InlineData("{\"rules\":[],\"defaultSettings\":{\"cryptoHash\":{},\"CryptoHash\":{}}}")]
        [InlineData("{\"rules\":[],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"SYNTHETIC-KEY\",\"CryptoHashKey\":\"SECOND\"}}}")]
        [InlineData("{\"rules\":[],\"customSettings\":{\"A\":{\"replaceWith\":\"FIRST\",\"ReplaceWith\":\"SECOND\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"cryptoHash\",\"params\":{\"cryptoHashKey\":\"SYNTHETIC-KEY\",\"CryptoHashKey\":\"SECOND\"}}]}")]
        public void GivenCaseDuplicateSemanticSettingFields_WhenParsing_ConfigurationIsRejectedWithoutValues(string json)
        {
            var error = Assert.Throws<AnonymizerConfigurationException>(() => AnonymizerConfigurationManager.CreateFromJson(json));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidConfigurationValues, error.DicomAnonymizerErrorCode);
            Assert.DoesNotContain("SYNTHETIC-KEY", error.ToString());
            Assert.DoesNotContain("SECOND", error.ToString());
        }

        [Fact]
        public void GivenDistinctCaseSensitiveSettingNames_WhenAnonymizing_SettingsRemainIndependent()
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"substitute\",\"setting\":\"A\"},{\"tag\":\"InstitutionName\",\"method\":\"substitute\",\"setting\":\"a\"}],\"customSettings\":{\"A\":{\"replaceWith\":\"FIRST\"},\"a\":{\"replaceWith\":\"SECOND\"}}}");
            var dataset = new DicomDataset { { DicomTag.PatientID, "ORIGINAL" }, { DicomTag.InstitutionName, "ORIGINAL" } };

            new AnonymizerEngine(manager).AnonymizeDataset(dataset);

            Assert.Equal("FIRST", dataset.GetString(DicomTag.PatientID));
            Assert.Equal("SECOND", dataset.GetString(DicomTag.InstitutionName));
        }

        [Fact]
        public void GivenMalformedSetting_WhenConstructingEngine_ErrorDoesNotExposeSettingsOrInnerValues()
        {
            var error = Assert.Throws<AnonymizerConfigurationException>(() => CreateEngine(
                Rule("PatientID", "cryptoHash", new JObject { ["cryptoHashKey"] = "SYNTHETIC-KEY", ["cryptoHashType"] = "SYNTHETIC-INVALID-ALGORITHM" })));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidRuleSettings, error.DicomAnonymizerErrorCode);
            Assert.DoesNotContain("SYNTHETIC", error.ToString());
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void GivenBroadSequenceRedaction_WhenConstructingEngine_PolicyIsRejected()
        {
            var error = Assert.Throws<AnonymizerConfigurationException>(() => CreateEngine(Rule("SQ", "redact")));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidConfigurationValues, error.DicomAnonymizerErrorCode);
        }

        [Fact]
        public void GivenSharedNestedDataset_WhenAnonymizingInPlace_EachValueIsTransformedOnce()
        {
            var shared = new DicomDataset { { DicomTag.PatientID, "ORIGINAL" } };
            var file = CreateFile(
                new DicomSequence(DicomTag.RequestAttributesSequence, shared),
                new DicomSequence(DicomTag.ContentSequence, shared));
            var scalar = new DicomDataset { { DicomTag.PatientID, "ORIGINAL" } };
            var engine = CreateEngine(Rule("PatientID", "cryptoHash"));

            engine.AnonymizeDataset(scalar);
            engine.AnonymizeFileInPlace(file);

            Assert.Same(shared, file.Dataset.GetSequence(DicomTag.RequestAttributesSequence).Items[0]);
            Assert.Same(shared, file.Dataset.GetSequence(DicomTag.ContentSequence).Items[0]);
            Assert.Equal(scalar.GetString(DicomTag.PatientID), shared.GetString(DicomTag.PatientID));
        }

        [Fact]
        public void GivenCaseDistinctNestedPrivateCreators_WhenAnonymizing_SelectorsRemainIndependent()
        {
            const string upperSelector = "(0011,1001:SYNTHETIC-CREATOR)";
            const string lowerSelector = "(0011,1001:synthetic-creator)";
            var upper = DicomTag.Parse(upperSelector);
            var lower = DicomTag.Parse(lowerSelector);
            var nested = new DicomDataset();
            nested.AddOrUpdate(DicomVR.LO, upper, "KEEP");
            nested.AddOrUpdate(DicomVR.LO, lower, "REPLACE");
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            var engine = CreateEngine(
                Rule(upperSelector, "keep"),
                Rule(lowerSelector, "substitute", new JObject { ["replaceWith"] = "ANONYMOUS" }));

            engine.AnonymizeFileInPlace(file);

            Assert.Equal("KEEP", nested.GetString(upper));
            Assert.Equal("ANONYMOUS", nested.GetString(lower));
        }

        private static JObject Rule(string selector, string method, JObject? parameters = null)
        {
            var rule = new JObject { ["tag"] = selector, ["method"] = method };
            if (parameters != null)
            {
                rule["params"] = parameters;
            }

            return rule;
        }

        private static AnonymizerEngine CreateEngine(params JObject[] rules)
        {
            return CreateEngine(new AnonymizerEngineOptions(), null, rules);
        }

        private static AnonymizerEngine CreateEngine(AnonymizerEngineOptions options, IAnonymizerProcessorFactory? factory, params JObject[] rules)
        {
            var manager = new AnonymizerConfigurationManager(new AnonymizerConfiguration
            {
                RuleContent = rules,
                DefaultSettings = new AnonymizerDefaultSettings
                {
                    CryptoHashDefaultSetting = new JObject { ["cryptoHashKey"] = "synthetic-key" },
                    RedactDefaultSetting = new JObject(),
                    DateShiftDefaultSetting = new JObject { ["dateShiftKey"] = "synthetic-key", ["dateShiftRange"] = 365 },
                },
                CustomSettings = new Dictionary<string, JObject>(),
            });
            return factory == null
                ? new AnonymizerEngine(manager, options)
                : new AnonymizerEngine(manager, options, processorFactory: factory);
        }

        private static DicomFile CreateFile(params DicomItem[] items)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
                new DicomOtherByte(DicomTag.PixelData, new byte[] { 1, 2, 3, 4 }),
            };
            dataset.Add(items);
            return new DicomFile(dataset);
        }

        private sealed class TrackingProcessor : IAnonymizerProcessor
        {
            public int Calls { get; private set; }

            public bool IsSupported(DicomItem item) => true;

            public void Process(DicomDataset dataset, DicomItem item, ProcessContext context)
            {
                Calls++;
            }
        }

        private sealed class ReplacingHashFactory : DicomProcessorFactory
        {
            private readonly IAnonymizerProcessor _processor;

            public ReplacingHashFactory(IAnonymizerProcessor processor)
            {
                _processor = processor;
            }

            public override IAnonymizerProcessor CreateProcessor(string method, JObject? settingObject = null)
            {
                if (method == "cryptoHash")
                {
                    return _processor;
                }

                return settingObject == null ? base.CreateProcessor(method) : base.CreateProcessor(method, settingObject);
            }
        }

        private sealed class UnreadableBuffer : IByteBuffer
        {
            public bool IsMemory => false;

            public long Size => 128 * 1024;

            public int ReadAttempts { get; private set; }

            public byte[] Data
            {
                get
                {
                    ReadAttempts++;
                    throw new InvalidOperationException("Unexpected bulk buffer read.");
                }
            }

            public void GetByteRange(long offset, int count, byte[] output)
            {
                ReadAttempts++;
                throw new InvalidOperationException("Unexpected bulk buffer read.");
            }

            public void CopyToStream(Stream stream)
            {
                ReadAttempts++;
                throw new InvalidOperationException("Unexpected bulk buffer read.");
            }

            public Task CopyToStreamAsync(Stream stream, CancellationToken cancellationToken)
            {
                ReadAttempts++;
                throw new InvalidOperationException("Unexpected bulk buffer read.");
            }
        }
    }
}
