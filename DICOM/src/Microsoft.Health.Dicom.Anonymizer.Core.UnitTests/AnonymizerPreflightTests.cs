// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerPreflightTests
    {
        public static IEnumerable<object[]> GetContextSelectionCases()
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                foreach (var requirement in new[] { "study", "series", "instance", "runtime-keys", "visited-earlier-rule", "no-runtime-keys" })
                {
                    yield return new object[] { mode, requirement };
                }
            }
        }

        public static IEnumerable<object[]> GetEntryPointAndBooleanCases()
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                yield return new object[] { mode, false };
                yield return new object[] { mode, true };
            }
        }

        public static IEnumerable<object[]> GetLongTextHashCases()
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                foreach (var tag in new[] { "LongCodeValue", "StrainAdditionalInformation" })
                {
                    yield return new object[] { mode, tag, 8192, "89024a7886bb0d706720a0fde6ba1b381310d7e39c764837c596db5dc8b2457b" };
                    yield return new object[] { mode, tag, 16384, "750c273ecdb4bc4ab4d14dcae50b38f48f98597415134be8f3dbbc9f67a0c81c" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetLongTextHashCases))]
        public void GivenLongTextWithCappedHashOutput_WhenAnonymizing_PreviousAcceptanceAndOutputArePreserved(string mode, string tagName, int length, string expected)
        {
            var tag = Assert.IsType<DicomTag>(typeof(DicomTag).GetField(tagName)?.GetValue(null));
            var input = new string('Z', length);
            var file = CreateFile();
            file.Dataset.Add(tag, input);
            file.Dataset.Add(DicomTag.PatientName, "SYNTHETIC^NAME");
            var vr = file.Dataset.GetDicomItem<DicomItem>(tag).ValueRepresentation;
            var engine = CreateEngine(
                Rule("PatientName", "cryptoHash"),
                Rule(tagName, "cryptoHash", new JObject { ["cryptoHashKey"] = "123", ["matchInputStringLength"] = true }));

            var result = AnonymizeUsingMode(engine, file, mode);

            Assert.Equal(expected, result.GetString(tag));
            Assert.Equal(64, result.GetString(tag).Length);
            Assert.Equal(vr, result.GetDicomItem<DicomItem>(tag).ValueRepresentation);
            var expectedName = HMACSHA256.HashData(Encoding.UTF8.GetBytes("synthetic-key"), Encoding.UTF8.GetBytes("SYNTHETIC^NAME"));
            Assert.Equal(Convert.ToHexString(expectedName).ToLowerInvariant(), result.GetString(DicomTag.PatientName));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, result.GetValues<byte>(DicomTag.PixelData));
            Assert.Equal(result.GetString(DicomTag.SOPInstanceUID), file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            if (mode == "clone")
            {
                Assert.Equal(input, file.Dataset.GetString(tag));
                Assert.Equal("SYNTHETIC^NAME", file.Dataset.GetString(DicomTag.PatientName));
            }
        }

        [Theory]
        [MemberData(nameof(GetEntryPointAndBooleanCases))]
        public void GivenActualDicomUnknownAfterAnEarlierRootRule_WhenHashing_ProcessingCompletesWithUnknownVr(string mode, bool removeName)
        {
            var tag = new DicomTag(0x7776, 0x1010);
            var sourceBytes = new byte[] { 1, 2, 3, 4 };
            var file = CreateFile(new DicomUnknown(tag, sourceBytes));
            file.Dataset.AddOrUpdate(DicomTag.PatientName, "SYNTHETIC^NAME");
            var expected = new DicomDataset { new DicomUnknown(tag, sourceBytes) };
            var processor = new CryptoHashProcessor(new JObject { ["cryptoHashKey"] = "synthetic-key" });
            processor.Process(expected, expected.GetDicomItem<DicomUnknown>(tag));
            var engine = CreateEngine(
                new AnonymizerEngineOptions(false, false),
                null,
                Rule("PatientName", removeName ? "remove" : "cryptoHash"),
                Rule("(7776,1010)", "cryptoHash"));

            var result = AnonymizeUsingMode(engine, file, mode);

            Assert.Equal(DicomVR.UN, Assert.IsType<DicomUnknown>(result.GetDicomItem<DicomItem>(tag)).ValueRepresentation);
            Assert.Equal(expected.GetValues<byte>(tag), result.GetValues<byte>(tag));
            Assert.Equal(!removeName, result.Contains(DicomTag.PatientName));
            if (!removeName)
            {
                Assert.NotEqual("SYNTHETIC^NAME", result.GetString(DicomTag.PatientName));
            }

            if (mode == "clone")
            {
                Assert.Equal(sourceBytes, file.Dataset.GetValues<byte>(tag));
                Assert.Equal("SYNTHETIC^NAME", file.Dataset.GetString(DicomTag.PatientName));
            }
        }

        [Theory]
        [MemberData(nameof(GetEntryPointAndBooleanCases))]
        public void GivenActualDicomUnknown_WhenKeepingOrRemoving_ExistingBehaviorIsPreserved(string mode, bool remove)
        {
            var tag = new DicomTag(0x7776, 0x1010);
            var sourceBytes = new byte[] { 1, 2, 3, 4 };
            var file = CreateFile(new DicomUnknown(tag, sourceBytes));

            var result = AnonymizeUsingMode(CreateEngine(Rule("(7776,1010)", remove ? "remove" : "keep")), file, mode);

            Assert.Equal(!remove, result.Contains(tag));
            if (!remove)
            {
                Assert.Equal(DicomVR.UN, Assert.IsType<DicomUnknown>(result.GetDicomItem<DicomItem>(tag)).ValueRepresentation);
                Assert.Equal(sourceBytes, result.GetValues<byte>(tag));
            }
        }

        [Theory]
        [MemberData(nameof(GetContextSelectionCases))]
        public void GivenContextualKeepBeforeUnsupportedFallback_WhenAnonymizing_ContextPreservesFirstMatch(string mode, string requirement)
        {
            var keys = requirement == "no-runtime-keys" ? null : new RuntimeKeySettings
            {
                CryptoHashKey = "synthetic-hash-key",
                DateShiftKey = "synthetic-date-key",
                EncryptKey = "synthetic-encrypt-key",
            };
            var keep = new ContextualKeepRule(requirement, keys);
            var engine = CreateCustomRuleEngine(
                new AnonymizerTagRule(DicomTag.PatientName, "keep", "Earlier name keep", new DicomProcessorFactory()),
                keep,
                new AnonymizerTagRule(DicomTag.PatientID, "refreshUID", "Unselected fallback", new DicomProcessorFactory()));
            var file = CreateContextFile();
            var sourceName = file.Dataset.GetString(DicomTag.PatientName);

            var output = AnonymizeUsingMode(engine, file, mode, keys);

            Assert.Equal("ROOT-IDENTIFIER", output.GetString(DicomTag.PatientID));
            var nested = Assert.Single(output.GetSequence(DicomTag.RequestAttributesSequence).Items);
            Assert.Equal("NESTED-IDENTIFIER", nested.GetString(DicomTag.PatientID));
            Assert.Equal(0, keep.MissingContexts);
            Assert.Contains(output, keep.MatchedDatasets);
            Assert.Contains(nested, keep.MatchedDatasets);
            Assert.Equal(sourceName, output.GetString(DicomTag.PatientName));
            Assert.Equal("2.25.100", output.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.2103", nested.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, output.GetValues<byte>(DicomTag.PixelData));
            Assert.Equal("ROOT-IDENTIFIER", file.Dataset.GetString(DicomTag.PatientID));
        }

        [Theory]
        [MemberData(nameof(GetEntryPointAndBooleanCases))]
        public void GivenNonmatchingRuntimeContext_WhenAnonymizing_UnsupportedFallbackRejectsBeforeMutation(string mode, bool supplyDifferentKeys)
        {
            var expectedKeys = new RuntimeKeySettings { CryptoHashKey = "synthetic-expected-key" };
            var suppliedKeys = supplyDifferentKeys
                ? new RuntimeKeySettings { CryptoHashKey = "synthetic-supplied-key" }
                : null;
            var keep = new ContextualKeepRule("runtime-keys", expectedKeys);
            var engine = CreateCustomRuleEngine(
                new AnonymizerTagRule(
                    DicomTag.PatientName,
                    "cryptoHash",
                    "Earlier transformation",
                    new DicomProcessorFactory(),
                    new JObject { ["cryptoHashKey"] = "synthetic-configured-key" }),
                keep,
                new AnonymizerTagRule(DicomTag.PatientID, "refreshUID", "Selected fallback", new DicomProcessorFactory()));
            var file = CreateContextFile();
            var originalName = file.Dataset.GetString(DicomTag.PatientName);

            var error = Assert.Throws<AnonymizerOperationException>(() => AnonymizeUsingMode(engine, file, mode, suppliedKeys));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.True(keep.MissingContexts > 0);
            Assert.Empty(keep.MatchedDatasets);
            Assert.Equal(originalName, file.Dataset.GetString(DicomTag.PatientName));
            Assert.Equal("ROOT-IDENTIFIER", file.Dataset.GetString(DicomTag.PatientID));
            Assert.Equal("NESTED-IDENTIFIER", Assert.Single(file.Dataset.GetSequence(DicomTag.RequestAttributesSequence).Items).GetString(DicomTag.PatientID));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, file.Dataset.GetValues<byte>(DicomTag.PixelData));
            Assert.DoesNotContain("synthetic-expected-key", error.ToString());
            Assert.DoesNotContain("synthetic-supplied-key", error.ToString());
            Assert.DoesNotContain("synthetic-configured-key", error.ToString());
            Assert.DoesNotContain("ROOT-IDENTIFIER", error.ToString());
        }

        [Theory]
        [MemberData(nameof(GetEntryPointAndBooleanCases))]
        public void GivenVisitationDependentSelection_WhenAnonymizing_OnlyEarlierDeclaredRulesProvideVisits(string mode, bool earlierKeep)
        {
            var keep = new ContextualKeepRule("visited-earlier-rule", null, DicomTag.PatientWeight);
            var weightRule = new AnonymizerTagRule(DicomTag.PatientWeight, "keep", "Weight keep", new DicomProcessorFactory());
            var fallback = new AnonymizerTagRule(DicomTag.PatientID, "refreshUID", "Context fallback", new DicomProcessorFactory());
            var engine = earlierKeep
                ? CreateCustomRuleEngine(weightRule, keep, fallback)
                : CreateCustomRuleEngine(keep, weightRule, fallback);
            var file = CreateContextFile();

            if (earlierKeep)
            {
                var output = AnonymizeUsingMode(engine, file, mode);

                Assert.Equal("ROOT-IDENTIFIER", output.GetString(DicomTag.PatientID));
                var nested = Assert.Single(output.GetSequence(DicomTag.RequestAttributesSequence).Items);
                Assert.Equal("NESTED-IDENTIFIER", nested.GetString(DicomTag.PatientID));
                Assert.Equal(0, keep.MissingContexts);
                Assert.Contains(output, keep.MatchedDatasets);
                Assert.Contains(nested, keep.MatchedDatasets);
            }
            else
            {
                var error = Assert.Throws<AnonymizerOperationException>(() => AnonymizeUsingMode(engine, file, mode));

                Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
                Assert.True(keep.MissingContexts > 0);
                Assert.Empty(keep.MatchedDatasets);
                Assert.Equal("ROOT-IDENTIFIER", file.Dataset.GetString(DicomTag.PatientID));
            }

            Assert.Equal("42", file.Dataset.GetString(DicomTag.PatientWeight));
        }

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
                new ReplacingBuiltInFactory(processor),
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
        public void GivenDataDependentMaskedRootMethod_WhenAnonymizing_EarlierRootRuleIsNotApplied()
        {
            var dataset = new DicomDataset { { DicomTag.PatientName, "Root^Person" }, { DicomTag.PatientID, "IDENTIFIER" } };
            var engine = CreateEngine(Rule("PatientName", "cryptoHash"), Rule("(0010,00xx)", "refreshUID"));

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

        [Fact]
        public void GivenAnonymizationErrors_WhenReadingCodes_ExistingNumericValuesArePreserved()
        {
            Assert.Equal(1001, (int)DicomAnonymizationErrorCode.ParsingJsonConfigurationFailed);
            Assert.Equal(1002, (int)DicomAnonymizationErrorCode.MissingConfigurationFields);
            Assert.Equal(1003, (int)DicomAnonymizationErrorCode.InvalidConfigurationValues);
            Assert.Equal(1004, (int)DicomAnonymizationErrorCode.UnsupportedAnonymizationRule);
            Assert.Equal(1005, (int)DicomAnonymizationErrorCode.MissingRuleSettings);
            Assert.Equal(1006, (int)DicomAnonymizationErrorCode.InvalidRuleSettings);
            Assert.Equal(1101, (int)DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod);
            Assert.Equal(1102, (int)DicomAnonymizationErrorCode.AddCustomProcessorFailed);
            Assert.Equal(1103, (int)DicomAnonymizationErrorCode.FileMetaIdentityMismatch);
            Assert.Equal(1104, (int)DicomAnonymizationErrorCode.SequenceDepthLimitExceeded);
            Assert.Equal(1105, (int)DicomAnonymizationErrorCode.SequenceExpansionLimitExceeded);
        }

        [Fact]
        public void GivenInPlacePreflightFailure_WhenAnonymizing_FileAndKeptBufferAreUnchanged()
        {
            var buffer = new UnreadableBuffer();
            var file = CreateFile();
            file.Dataset.AddOrUpdate(new DicomOtherByte(DicomTag.PixelData, buffer));
            file.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUID.Parse("2.25.999");
            var error = Assert.Throws<AnonymizerOperationException>(() =>
                CreateEngine(Rule("SOPInstanceUID", "refreshUID")).AnonymizeFileInPlace(file));

            Assert.Equal(DicomAnonymizationErrorCode.FileMetaIdentityMismatch, error.DicomAnonymizerErrorCode);
            Assert.Equal("2.25.100", file.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.999", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(0, buffer.ReadAttempts);
        }

        [Theory]
        [MemberData(nameof(GetEntryPointAndBooleanCases))]
        public async Task GivenCyclicSequence_WhenAnonymizing_DepthRejectionTerminatesWithoutMutationAsync(string mode, bool indirectCycle)
        {
            var buffer = new UnreadableBuffer();
            var file = CreateFile(new DicomLongString(DicomTag.PatientID, "UNCHANGED"));
            file.Dataset.AddOrUpdate(new DicomOtherByte(DicomTag.PixelData, buffer));
            var child = indirectCycle ? new DicomDataset() : file.Dataset;
            if (indirectCycle)
            {
                child.Add(new DicomSequence(DicomTag.ContentSequence, file.Dataset));
            }

            file.Dataset.Add(new DicomSequence(DicomTag.RequestAttributesSequence, child));
            var engine = CreateEngine(Rule("PatientID", "cryptoHash"));

            var error = await AssertCyclicInputRejectedAsync(engine, file, mode);

            Assert.Equal(DicomAnonymizationErrorCode.SequenceDepthLimitExceeded, error.DicomAnonymizerErrorCode);
            Assert.Equal("UNCHANGED", file.Dataset.GetString(DicomTag.PatientID));
            Assert.Equal("2.25.100", file.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.100", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Same(child, Assert.Single(file.Dataset.GetSequence(DicomTag.RequestAttributesSequence).Items));
            Assert.Same(buffer, file.Dataset.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer);
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
        [InlineData("ReferencedSOPClassUID", "ReferencedSOPClassUID")]
        [InlineData("SOPClassesInStudy", "SOPClassesInStudy")]
        [InlineData("(0008,1150)", "ReferencedSOPClassUID")]
        [InlineData("00080062", "SOPClassesInStudy")]
        [InlineData("(0008,11xx)", "ReferencedSOPClassUID")]
        public void GivenClassUidSelector_WhenAnonymizingAnInvariantValue_PreflightRejectsBeforeMutation(string selector, string tagName)
        {
            var tag = Assert.IsType<DicomTag>(typeof(DicomTag).GetField(tagName)?.GetValue(null));
            var dataset = new DicomDataset { { tag, DicomUID.CTImageStorage }, { DicomTag.PatientID, "UNCHANGED" } };
            var engine = CreateEngine(Rule("PatientID", "cryptoHash"), Rule(selector, "refreshUID"));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Equal(DicomUID.CTImageStorage.UID, dataset.GetString(tag));
            Assert.Equal("UNCHANGED", dataset.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("{\"rules\":[],\"defaultSettings\":{\"cryptoHash\":{},\"CryptoHash\":{}}}")]
        [InlineData("{\"rules\":[],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"SYNTHETIC-KEY\",\"CryptoHashKey\":\"SECOND\"}}}")]
        [InlineData("{\"rules\":[],\"customSettings\":{\"A\":{\"replaceWith\":\"FIRST\",\"ReplaceWith\":\"SECOND\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"cryptoHash\",\"params\":{\"cryptoHashKey\":\"SYNTHETIC-KEY\",\"CryptoHashKey\":\"SECOND\"}}]}")]
        public void GivenCaseVariantSettingFields_WhenParsing_ExistingSerializerBehaviorIsAllowed(string json)
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(json);

            Assert.NotNull(new AnonymizerEngine(manager));
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

        [Theory]
        [InlineData("remove")]
        [InlineData("redact")]
        public void GivenInvariantKeepsAndBroadRules_WhenAnonymizing_CompatibleItemsRespectFirstMatch(string sequenceMethod)
        {
            var removedChild = new DicomDataset { { DicomTag.PatientID, "DISCARD" } };
            var keptChild = new DicomDataset { { DicomTag.Manufacturer, "UNCHANGED" } };
            var file = CreateFile(
                new DicomSequence(DicomTag.ContentSequence, removedChild),
                new DicomSequence(DicomTag.RequestAttributesSequence, keptChild));
            file.Dataset.Add(DicomTag.SOPClassesInStudy, DicomUID.CTImageStorage);
            file.Dataset.Add(DicomTag.ReferencedSOPClassUID, DicomUID.CTImageStorage);
            file.Dataset.Add(DicomTag.InstanceCreatorUID, "2.25.200");
            var originalSyntax = file.FileMetaInfo.TransferSyntax;
            var engine = CreateEngine(
                Rule("SOPClassUID", "keep"),
                Rule("SOPClassesInStudy", "keep"),
                Rule("ReferencedSOPClassUID", "keep"),
                Rule("RequestAttributesSequence", "keep"),
                Rule("UI", "refreshUID"),
                Rule("SQ", sequenceMethod));

            var output = engine.AnonymizeFile(file);

            Assert.Equal(DicomUID.CTImageStorage, output.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID));
            Assert.Equal(DicomUID.CTImageStorage.UID, output.Dataset.GetString(DicomTag.SOPClassesInStudy));
            Assert.Equal(DicomUID.CTImageStorage.UID, output.Dataset.GetString(DicomTag.ReferencedSOPClassUID));
            Assert.NotEqual("2.25.100", output.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.NotEqual("2.25.200", output.Dataset.GetString(DicomTag.InstanceCreatorUID));
            Assert.Equal(output.Dataset.GetString(DicomTag.SOPInstanceUID), output.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(originalSyntax, output.FileMetaInfo.TransferSyntax);
            Assert.Equal("UNCHANGED", Assert.Single(output.Dataset.GetSequence(DicomTag.RequestAttributesSequence).Items).GetString(DicomTag.Manufacturer));
            if (sequenceMethod == "remove")
            {
                Assert.False(output.Dataset.Contains(DicomTag.ContentSequence));
            }
            else
            {
                Assert.Empty(output.Dataset.GetSequence(DicomTag.ContentSequence).Items);
            }

            Assert.Equal(new byte[] { 1, 2, 3, 4 }, output.Dataset.GetValues<byte>(DicomTag.PixelData));
            Assert.Equal("2.25.100", file.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal("DISCARD", Assert.Single(file.Dataset.GetSequence(DicomTag.ContentSequence).Items).GetString(DicomTag.PatientID));
        }

        [Fact]
        public void GivenMaskedHashWithEarlierExactKeep_WhenAnonymizing_ActualSupportedValuesAreProcessed()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.PatientName, "UNCHANGED" },
                { DicomTag.PatientID, "TRANSFORM" },
            };
            var engine = CreateEngine(Rule("PatientName", "keep"), Rule("(0010,00xx)", "cryptoHash"));

            engine.AnonymizeDataset(dataset);

            Assert.Equal("UNCHANGED", dataset.GetString(DicomTag.PatientName));
            Assert.Matches("^[0-9a-f]{64}$", dataset.GetString(DicomTag.PatientID));
            dataset.Validate();
        }

        [Fact]
        public void GivenUnknownExactHashTag_WhenAnonymizing_ActualStringRepresentationIsSupported()
        {
            var tag = new DicomTag(0x7776, 0x1010);
            var dataset = new DicomDataset { new DicomLongString(tag, "TRANSFORM") };
            var engine = CreateEngine(Rule("(7776,1010)", "cryptoHash"));

            engine.AnonymizeDataset(dataset);

            Assert.Equal(DicomVR.LO, dataset.GetDicomItem<DicomItem>(tag).ValueRepresentation);
            Assert.Matches("^[0-9a-f]{64}$", dataset.GetString(tag));
        }

        [Fact]
        public void GivenDuplicateExactSubstitutions_WhenAnonymizing_RootAndNestedUseOnlyFirstRule()
        {
            var nested = new DicomDataset { { DicomTag.PatientID, "NESTED" } };
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "ROOT" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nested),
            };
            var engine = CreateEngine(
                Rule("PatientID", "substitute", new JObject { ["replaceWith"] = "FIRST" }),
                Rule("(0010,0020)", "substitute", new JObject { ["replaceWith"] = "SECOND" }));

            engine.AnonymizeDataset(dataset);

            Assert.Equal("FIRST", dataset.GetString(DicomTag.PatientID));
            Assert.Equal("FIRST", nested.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("PatientID")]
        [InlineData("LO")]
        public void GivenUnsupportedActualUidRefreshItem_WhenAnonymizing_PreflightStillPreservesInput(string selector)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.PatientName, "UNCHANGED" },
                { DicomTag.PatientID, "NOT-A-UID" },
            };
            var engine = CreateEngine(Rule("PatientName", "cryptoHash"), Rule(selector, "refreshUID"));

            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Equal("UNCHANGED", dataset.GetString(DicomTag.PatientName));
            Assert.Equal("NOT-A-UID", dataset.GetString(DicomTag.PatientID));
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

        [Theory]
        [InlineData("dataset")]
        [InlineData("inplace")]
        [InlineData("clone")]
        public void GivenPrivateDataRuleBeforeCreatorRemoval_WhenAnonymizing_RootAndNestedRulesKeepDeclaredOrder(string mode)
        {
            const string relativeSelector = "(0011,0001:SYNTHETIC-CREATOR)";
            var tag = DicomTag.Parse(relativeSelector);
            var scalar = CreateRelativePrivateDataset(tag);
            var nested = CreateRelativePrivateDataset(tag);
            var root = CreateRelativePrivateDataset(tag);
            root.Add(DicomTag.SOPClassUID, DicomUID.CTImageStorage);
            root.Add(DicomTag.SOPInstanceUID, "2.25.100");
            root.Add(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            var engine = CreateEngine(Rule(relativeSelector, "remove"), Rule("(0011,0010)", "remove"));

            engine.AnonymizeDataset(scalar);
            DicomDataset output;
            if (mode == "clone")
            {
                output = engine.AnonymizeFile(new DicomFile(root)).Dataset;
                Assert.Contains(root, item => item.Tag.Group == 0x0011 && item.Tag.Element == 0x1001);
                Assert.Contains(nested, item => item.Tag.Group == 0x0011 && item.Tag.Element == 0x1001);
            }
            else
            {
                if (mode == "inplace")
                {
                    engine.AnonymizeFileInPlace(new DicomFile(root));
                }
                else
                {
                    engine.AnonymizeDataset(root);
                }

                output = root;
            }

            var outputNested = Assert.Single(output.GetSequence(DicomTag.RequestAttributesSequence).Items);
            Assert.DoesNotContain(scalar, item => item.Tag.IsPrivate);
            Assert.DoesNotContain(output, item => item.Tag.IsPrivate);
            Assert.DoesNotContain(outputNested, item => item.Tag.IsPrivate);
            Assert.Equal("UNCHANGED", scalar.GetString(DicomTag.Manufacturer));
            Assert.Equal("UNCHANGED", output.GetString(DicomTag.Manufacturer));
            Assert.Equal("UNCHANGED", outputNested.GetString(DicomTag.Manufacturer));
        }

        [Theory]
        [InlineData("PatientID", "refreshUID")]
        [InlineData("(0010,0020)", "refreshUID")]
        [InlineData("LO", "refreshUID")]
        [InlineData("PatientName", "dateShift")]
        [InlineData("PN", "dateShift")]
        [InlineData("PatientName", "perturb")]
        [InlineData("PN", "perturb")]
        [InlineData("Rows", "encrypt")]
        [InlineData("US", "encrypt")]
        [InlineData("FileMetaInformationVersion", "substitute")]
        [InlineData("OB", "substitute")]
        [InlineData("ContentSequence", "substitute")]
        public void GivenBuiltInSelector_WhenNoMatchingDataExists_ConstructionAndProcessingDoNotRejectTheConfiguration(string selector, string method)
        {
            var engine = CreateEngine(Rule(selector, method, new JObject { ["encryptKey"] = "0123456789ABCDEF" }));
            var dataset = new DicomDataset();

            engine.AnonymizeDataset(dataset);

            Assert.Empty(dataset);
        }

        [Theory]
        [InlineData("PatientID", "redact")]
        [InlineData("LO", "redact")]
        [InlineData("InstanceCreatorUID", "refreshUID")]
        [InlineData("StudyDate", "dateShift")]
        [InlineData("DA", "dateShift")]
        [InlineData("Rows", "perturb")]
        [InlineData("US", "perturb")]
        [InlineData("PatientName", "encrypt")]
        [InlineData("PN", "encrypt")]
        [InlineData("ContentSequence", "remove")]
        [InlineData("ContentSequence", "redact")]
        [InlineData("PixelData", "substitute")]
        [InlineData("PixelData", "encrypt")]
        public void GivenCompatibleOrDataDependentBuiltInSelector_WhenConstructingEngine_PolicyIsAccepted(string selector, string method)
        {
            var engine = CreateEngine(Rule(selector, method, new JObject { ["encryptKey"] = "0123456789ABCDEF" }));

            Assert.NotNull(engine);
        }

        [Fact]
        public void GivenUnknownExactTag_WhenConstructingEngine_ActualVrIsCheckedAtRuntime()
        {
            var tag = new DicomTag(0x7776, 0x1010);
            Assert.Same(DicomDictionary.UnknownTag, tag.DictionaryEntry);
            var engine = CreateEngine(Rule("(7776,1010)", "refreshUID"));
            var supported = new DicomDataset { new DicomUniqueIdentifier(tag, "2.25.700") };

            engine.AnonymizeDataset(supported);

            Assert.NotEqual("2.25.700", supported.GetString(tag));
            var unsupported = new DicomDataset { new DicomLongString(tag, "UNCHANGED") };
            var error = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(unsupported));
            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.Equal("UNCHANGED", unsupported.GetString(tag));
        }

        [Fact]
        public void GivenCustomProcessorUsingBuiltInName_WhenConstructingEngine_BuiltInCapabilitiesAreNotAssumed()
        {
            var processor = new TrackingProcessor();
            var engine = CreateEngine(
                new AnonymizerEngineOptions(),
                new ReplacingBuiltInFactory(processor, "refreshUID"),
                Rule("PatientID", "refreshUID"));

            engine.AnonymizeDataset(new DicomDataset { { DicomTag.PatientID, "UNCHANGED" } });

            Assert.Equal(1, processor.Calls);
        }

        [Theory]
        [InlineData("dataset", false)]
        [InlineData("inplace", false)]
        [InlineData("clone", false)]
        [InlineData("dataset", true)]
        [InlineData("inplace", true)]
        [InlineData("clone", true)]
        public void GivenPrivateRootSelectorWithoutNestedCreator_WhenAnonymizing_OnlyPresentCandidatesAreMatched(string mode, bool nestedExactCandidate)
        {
            const string selector = "(0011,0001:SYNTHETIC-ROOT-CREATOR)";
            var tag = DicomTag.Parse(selector);
            var nested = new DicomDataset { { DicomTag.Manufacturer, "NESTED-UNCHANGED" } };
            if (nestedExactCandidate)
            {
                nested.Add(DicomTag.PatientID, "NESTED-IDENTIFIER");
            }

            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            file.Dataset.AddOrUpdate(DicomVR.LO, tag, "SYNTHETIC-ROOT-PRIVATE-VALUE");
            var engine = CreateEngine(
                Rule(selector, "remove"),
                Rule("(0011,0010)", "remove"),
                Rule("PatientID", "substitute", new JObject { ["replaceWith"] = "ANONYMOUS" }));

            var output = AnonymizeUsingMode(engine, file, mode);

            Assert.DoesNotContain(output, item => item.Tag.IsPrivate);
            var outputNested = Assert.Single(output.GetSequence(DicomTag.RequestAttributesSequence).Items);
            Assert.DoesNotContain(outputNested, item => item.Tag.IsPrivate);
            Assert.Equal("NESTED-UNCHANGED", outputNested.GetString(DicomTag.Manufacturer));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, output.GetValues<byte>(DicomTag.PixelData));
            if (nestedExactCandidate)
            {
                Assert.Equal("ANONYMOUS", outputNested.GetString(DicomTag.PatientID));
            }
            else
            {
                Assert.False(outputNested.Contains(DicomTag.PatientID));
            }

            if (mode == "clone")
            {
                Assert.Equal("SYNTHETIC-ROOT-PRIVATE-VALUE", file.Dataset.GetString(tag));
                Assert.Equal(nestedExactCandidate, nested.Contains(DicomTag.PatientID));
                if (nestedExactCandidate)
                {
                    Assert.Equal("NESTED-IDENTIFIER", nested.GetString(DicomTag.PatientID));
                }
            }
        }

        [Theory]
        [InlineData("dataset", false)]
        [InlineData("inplace", false)]
        [InlineData("clone", false)]
        [InlineData("dataset", true)]
        [InlineData("inplace", true)]
        [InlineData("clone", true)]
        public void GivenRootOnlyCustomRule_WhenNoExactNestedCandidateExists_CustomSelectorIsNotInvokedOnNestedData(string mode, bool unmatchedExactRule)
        {
            var custom = new RootOnlyRule();
            var rules = new List<AnonymizerRule> { custom };
            if (unmatchedExactRule)
            {
                rules.Add(new AnonymizerTagRule(DicomTag.StudyDescription, "keep", "Root description", new DicomProcessorFactory()));
            }

            var manager = new AnonymizerConfigurationManager(new AnonymizerConfiguration { RuleContent = Array.Empty<JObject>() });
            var engine = new AnonymizerEngine(manager, ruleFactory: new SuppliedRuleFactory(rules.ToArray()));
            var nested = new DicomDataset { { DicomTag.PatientID, "NESTED-UNCHANGED" } };
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            file.Dataset.Add(DicomTag.PatientName, "SYNTHETIC^ROOT");
            file.Dataset.Add(DicomTag.StudyDescription, "ROOT-UNCHANGED");

            var output = AnonymizeUsingMode(engine, file, mode);

            Assert.True(custom.RootCalls > 0);
            Assert.Equal(0, custom.NestedCalls);
            Assert.Equal("SYNTHETIC^ROOT", output.GetString(DicomTag.PatientName));
            Assert.Equal("ROOT-UNCHANGED", output.GetString(DicomTag.StudyDescription));
            Assert.Equal("NESTED-UNCHANGED", Assert.Single(output.GetSequence(DicomTag.RequestAttributesSequence).Items).GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("dataset")]
        [InlineData("inplace")]
        [InlineData("clone")]
        public void GivenPrivateTargetBelowUnselectedContainers_WhenAnonymizing_TraversalStillReachesAndProcessesIt(string mode)
        {
            const string selector = "(0011,0001:SYNTHETIC-CREATOR)";
            var tag = DicomTag.Parse(selector);
            var target = CreateRelativePrivateDataset(tag);
            var middle = new DicomDataset
            {
                { DicomTag.Manufacturer, "MIDDLE-UNCHANGED" },
                new DicomSequence(DicomTag.ContentSequence, target),
            };
            var unrelated = new DicomDataset { { DicomTag.Manufacturer, "SIBLING-UNCHANGED" } };
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, middle, unrelated));
            file.Dataset.AddOrUpdate(DicomVR.LO, tag, "ROOT-PRIVATE");
            var engine = CreateEngine(Rule(selector, "remove"), Rule("(0011,0010)", "remove"));

            var output = AnonymizeUsingMode(engine, file, mode);

            Assert.DoesNotContain(output, item => item.Tag.IsPrivate);
            var items = output.GetSequence(DicomTag.RequestAttributesSequence).Items;
            Assert.Equal(2, items.Count);
            Assert.Equal("MIDDLE-UNCHANGED", items[0].GetString(DicomTag.Manufacturer));
            Assert.Equal("SIBLING-UNCHANGED", items[1].GetString(DicomTag.Manufacturer));
            var outputTarget = Assert.Single(items[0].GetSequence(DicomTag.ContentSequence).Items);
            Assert.DoesNotContain(outputTarget, item => item.Tag.IsPrivate);
            Assert.Equal("UNCHANGED", outputTarget.GetString(DicomTag.Manufacturer));
            if (mode == "clone")
            {
                Assert.Equal("ROOT-PRIVATE", file.Dataset.GetString(tag));
                Assert.Equal("SYNTHETIC-PRIVATE-IDENTIFIER", target.GetString(tag));
            }
        }

        [Fact]
        public void GivenNestedPrivateCreatorWithDifferentCase_WhenAnonymizing_AbsentSelectorDoesNotMatchNumericBlock()
        {
            const string upperSelector = "(0011,0001:SYNTHETIC-CREATOR)";
            const string lowerSelector = "(0011,0001:synthetic-creator)";
            var upper = DicomTag.Parse(upperSelector);
            var lower = DicomTag.Parse(lowerSelector);
            var nested = CreateRelativePrivateDataset(lower);
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            file.Dataset.AddOrUpdate(DicomVR.LO, upper, "ROOT-PRIVATE");
            var engine = CreateEngine(Rule(upperSelector, "remove"), Rule("LO", "keep"));

            engine.AnonymizeFileInPlace(file);

            Assert.False(file.Dataset.Contains(upper));
            Assert.False(nested.Contains(upper));
            Assert.Equal("SYNTHETIC-PRIVATE-IDENTIFIER", nested.GetString(lower));
            Assert.Equal("synthetic-creator", nested.GetString(new DicomTag(0x0011, 0x0010)));
        }

        private static async Task<AnonymizerOperationException> AssertCyclicInputRejectedAsync(AnonymizerEngine engine, DicomFile file, string mode)
        {
            var operation = Task.Run(() => Assert.Throws<AnonymizerOperationException>(() => AnonymizeUsingMode(engine, file, mode)));
            var completed = await Task.WhenAny(operation, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(operation, completed);
            return await operation;
        }

        private static DicomDataset AnonymizeUsingMode(AnonymizerEngine engine, DicomFile file, string mode, RuntimeKeySettings? runtimeKeys = null)
        {
            if (mode == "clone")
            {
                return runtimeKeys == null
                    ? engine.AnonymizeFile(file).Dataset
                    : engine.AnonymizeFile(file, runtimeKeys).Dataset;
            }

            if (mode == "inplace")
            {
                if (runtimeKeys == null)
                {
                    engine.AnonymizeFileInPlace(file);
                }
                else
                {
                    engine.AnonymizeFileInPlace(file, runtimeKeys);
                }
            }
            else
            {
                Assert.Equal("dataset", mode);
                if (runtimeKeys == null)
                {
                    engine.AnonymizeDataset(file.Dataset);
                }
                else
                {
                    engine.AnonymizeDataset(file.Dataset, runtimeKeys);
                }
            }

            return file.Dataset;
        }

        private static AnonymizerEngine CreateCustomRuleEngine(params AnonymizerRule[] rules)
        {
            var configuration = new AnonymizerConfigurationManager(new AnonymizerConfiguration { RuleContent = Array.Empty<JObject>() });
            return new AnonymizerEngine(configuration, ruleFactory: new SuppliedRuleFactory(rules));
        }

        private static DicomFile CreateContextFile()
        {
            var nested = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "2.25.2101" },
                { DicomTag.SeriesInstanceUID, "2.25.2102" },
                { DicomTag.SOPInstanceUID, "2.25.2103" },
                { DicomTag.PatientName, "Synthetic^Nested" },
                { DicomTag.PatientID, "NESTED-IDENTIFIER" },
                { DicomTag.PatientWeight, "21" },
            };
            var file = CreateFile(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
            file.Dataset.Add(DicomTag.StudyInstanceUID, "2.25.1101");
            file.Dataset.Add(DicomTag.SeriesInstanceUID, "2.25.1102");
            file.Dataset.Add(DicomTag.PatientName, "Synthetic^Root");
            file.Dataset.Add(DicomTag.PatientID, "ROOT-IDENTIFIER");
            file.Dataset.Add(DicomTag.PatientWeight, "42");
            return file;
        }

        private static DicomDataset CreateRelativePrivateDataset(DicomTag tag)
        {
            var dataset = new DicomDataset { { DicomTag.Manufacturer, "UNCHANGED" } };
            dataset.AddOrUpdate(DicomVR.LO, tag, "SYNTHETIC-PRIVATE-IDENTIFIER");
            Assert.Contains(dataset, item => item.Tag.Group == 0x0011 && item.Tag.Element == 0x1001);
            Assert.Contains(dataset, item => item.Tag.Group == 0x0011 && item.Tag.Element == 0x0010);
            return dataset;
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

        private sealed class ContextualKeepRule : AnonymizerRule
        {
            private readonly string _requirement;
            private readonly RuntimeKeySettings? _expectedKeys;
            private readonly DicomTag _visitedTag;

            public ContextualKeepRule(string requirement, RuntimeKeySettings? expectedKeys, DicomTag? visitedTag = null)
                : base("keep", "Contextual keep", new DicomProcessorFactory())
            {
                _requirement = requirement;
                _expectedKeys = expectedKeys;
                _visitedTag = visitedTag ?? DicomTag.PatientName;
            }

            public int MissingContexts { get; private set; }

            public HashSet<DicomDataset> MatchedDatasets { get; } = new HashSet<DicomDataset>(ReferenceEqualityComparer.Instance);

            public override List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
            {
                var matches = _requirement switch
                {
                    "study" => context.StudyInstanceUID == dataset.GetString(DicomTag.StudyInstanceUID),
                    "series" => context.SeriesInstanceUID == dataset.GetString(DicomTag.SeriesInstanceUID),
                    "instance" => context.SopInstanceUID == dataset.GetString(DicomTag.SOPInstanceUID),
                    "runtime-keys" => ReferenceEquals(context.RuntimeKeys, _expectedKeys),
                    "visited-earlier-rule" => context.VisitedNodes.Contains(dataset.GetDicomItem<DicomItem>(_visitedTag).ToString()),
                    "no-runtime-keys" => context.RuntimeKeys == null &&
                        context.StudyInstanceUID == dataset.GetString(DicomTag.StudyInstanceUID) &&
                        context.SeriesInstanceUID == dataset.GetString(DicomTag.SeriesInstanceUID) &&
                        context.SopInstanceUID == dataset.GetString(DicomTag.SOPInstanceUID),
                    _ => throw new InvalidOperationException("Unknown context test requirement."),
                };
                if (!matches)
                {
                    MissingContexts++;
                    return new List<DicomItem>();
                }

                MatchedDatasets.Add(dataset);
                var item = dataset.GetDicomItem<DicomItem>(DicomTag.PatientID);
                return item == null || context.VisitedNodes.Contains(item.ToString())
                    ? new List<DicomItem>()
                    : new List<DicomItem> { item };
            }
        }

        private sealed class RootOnlyRule : AnonymizerRule
        {
            public RootOnlyRule()
                : base("keep", "Root-only selection", new DicomProcessorFactory())
            {
            }

            public int RootCalls { get; private set; }

            public int NestedCalls { get; private set; }

            public override List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
            {
                if (!dataset.Contains(DicomTag.SOPClassUID))
                {
                    NestedCalls++;
                    throw new InvalidOperationException("Root-only selector invoked on a nested dataset.");
                }

                RootCalls++;
                var item = dataset.GetDicomItem<DicomItem>(DicomTag.PatientName);
                return item == null ? new List<DicomItem>() : new List<DicomItem> { item };
            }
        }

        private sealed class SuppliedRuleFactory : IAnonymizerRuleFactory
        {
            private readonly AnonymizerRule[] _rules;

            public SuppliedRuleFactory(AnonymizerRule[] rules)
            {
                _rules = rules;
            }

            public AnonymizerRule[] CreateDicomAnonymizationRules(JObject[] content) => _rules;

            public AnonymizerRule CreateDicomAnonymizationRule(JObject content) => throw new NotSupportedException();
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

        private sealed class ReplacingBuiltInFactory : DicomProcessorFactory
        {
            private readonly IAnonymizerProcessor _processor;

            private readonly string _method;

            public ReplacingBuiltInFactory(IAnonymizerProcessor processor, string method = "cryptoHash")
            {
                _processor = processor;
                _method = method;
            }

            public override IAnonymizerProcessor CreateProcessor(string method, JObject? settingObject = null)
            {
                if (method == _method)
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
