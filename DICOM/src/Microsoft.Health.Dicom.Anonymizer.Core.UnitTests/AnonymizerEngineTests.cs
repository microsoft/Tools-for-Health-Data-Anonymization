// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        public AnonymizerEngineTests()
        {
            Dataset = new DicomDataset()
            {
                { DicomTag.SOPClassUID, "2.25.4116340212742820117545040869474001540" },
                { DicomTag.SOPInstanceUID, "2.25.322924430372144810477559413499190252923" },
                { DicomTag.RetrieveAETitle, "TEST" }, // AE
                { DicomTag.PatientAge, "100Y" },  // AS
                { DicomTag.Query​Retrieve​Level, "0" }, // CS
                { DicomTag.Event​Elapsed​Times, "1234.5" }, // DS
                { DicomTag.Stage​Number, "1234" }, // IS
                { DicomTag.Patient​Telephone​Numbers, "TEST" }, // SH
                { DicomTag.SOP​Classes​In​Study, "12345" }, // UI
                { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }, // FD
                { DicomTag.Examined​Body​Thickness, "12345" }, // FL
                { DicomTag.Doppler​Sample​Volume​X​Position, "12345" }, // SL
                { DicomTag.Real​World​Value​First​Value​Mapped, "12345" }, // SS
                { DicomTag.Referenced​Content​Item​Identifier, "12345" }, // UL
                { DicomTag.Referenced​Waveform​Channels, "12345\\1234" }, // US
                { DicomTag.Consulting​Physician​Name, "Test\\Test" }, // PN
                { DicomTag.Long​Code​Value, "TEST" }, // UC
                { DicomTag.Event​Timer​Names, "TestTimer" }, // LO
                { DicomTag.Strain​Additional​Information, "TestInformation" }, // UT
                { DicomTag.Derivation​Description, "TestDescription" }, // ST
                { DicomTag.VisitComments, "TestComments" }, // LT
                { DicomTag.Pixel​Data​Provider​URL, "http://test" }, // UR
                { DicomTag.SubtractionItemID, "123" },  // US
                { DicomTag.Warning​Reason, "10" },  // FS
            };
        }

        public DicomDataset Dataset { get; set; }

        [Fact]
        public void GivenDicomDataSet_SetValidateOutput_WhenAnonymizeWithInvalidOutput_ExceptionWillBeThrown()
        {
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-invalid-string-output.json", new AnonymizerEngineOptions(validateOutput: true));
            Assert.Throws<DicomValidationException>(() => engine.AnonymizeDataset(Dataset));
        }

        [Fact]
        public void GivenInvalidDicomDataSet_SetValidateInput_ExceptionWillBeThrown()
        {
            DicomUtility.DisableAutoValidation(Dataset);
            Dataset.AddOrUpdate(DicomTag.PatientAge, "invalid");
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-invalid-string-output.json", new AnonymizerEngineOptions(validateInput: true));
            Assert.Throws<DicomValidationException>(() => engine.AnonymizeDataset(Dataset));
        }

        [Fact]
        public void GivenDicomDataSet_UsingCustomProcessor_WhenAnonymize_ValidDicomDatasetWillBeReturned()
        {
            var processorFactory = new CustomProcessorFactory();
            processorFactory.RegisterProcessors(typeof(MaskProcessor));

            var engine = new AnonymizerEngine("./TestConfigurations/configuration-custom.json", processorFactory: processorFactory);

            engine.AnonymizeDataset(Dataset);

            var dicomFile = DicomFile.Open("DicomResults/custom.dcm");
            foreach (var item in Dataset)
            {
                Assert.Equal(((DicomElement)item).Get<string>(), dicomFile.Dataset.GetString(item.Tag));
            }
        }

        [Fact]
        public void GivenDicomDataSet_SetValidationOutput_WhenAnonymize_ValidDicomDatasetWillBeReturned()
        {
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json", new AnonymizerEngineOptions(validateOutput: true));
            engine.AnonymizeDataset(Dataset);
            var dicomFile = DicomFile.Open("DicomResults/anonymized.dcm");
            foreach (var item in Dataset)
            {
                Assert.Equal(((DicomElement)item).Get<string>(), dicomFile.Dataset.GetString(item.Tag));
            }
        }

        [Fact]
        public void GivenDicomDataSet_SetValidationOutputFalse_WhenAnonymizeWithInvalidOutput_InvalidDicomDatasetWillBeReturned()
        {
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-invalid-string-output.json", new AnonymizerEngineOptions(false, false));
            engine.AnonymizeDataset(Dataset);
            var dicomFile = DicomFile.Open("DicomResults/Invalid-String-Format.dcm");
            foreach (var item in Dataset)
            {
                Assert.Equal(((DicomElement)item).Get<string>(), dicomFile.Dataset.GetString(item.Tag));
            }
        }

        [Fact]
        public void GivenFile_WhenSopInstanceUidIsRefreshed_FileMetaIdentityIsSynchronizedAndInvariantsArePreserved()
        {
            const string originalInstanceUid = "2.25.100";
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, originalInstanceUid },
                { DicomTag.ReferencedSOPInstanceUID, originalInstanceUid },
            };
            var file = new DicomFile(dataset);
            var originalTransferSyntax = file.FileMetaInfo.TransferSyntax;
            var engine = CreateEngine(
                new JObject { ["tag"] = "SOPInstanceUID", ["method"] = "refreshUID" },
                new JObject { ["tag"] = "ReferencedSOPInstanceUID", ["method"] = "refreshUID" });

            var output = engine.AnonymizeFile(file);

            var refreshedUid = output.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID);
            Assert.NotEqual(originalInstanceUid, refreshedUid.UID);
            Assert.Equal(refreshedUid, output.FileMetaInfo.MediaStorageSOPInstanceUID);
            Assert.Equal(refreshedUid, output.Dataset.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID));
            Assert.Equal(DicomUID.CTImageStorage, output.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID));
            Assert.Equal(DicomUID.CTImageStorage, output.FileMetaInfo.MediaStorageSOPClassUID);
            Assert.Equal(originalTransferSyntax, output.FileMetaInfo.TransferSyntax);
            Assert.Equal(originalInstanceUid, file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
        }

        [Theory]
        [InlineData((64 * 1024) - 2)]
        [InlineData(64 * 1024)]
        [InlineData((64 * 1024) + 2)]
        public async Task GivenKeptBinaryValueNearLargeObjectThreshold_WhenAnonymizing_FileRemainsReadableAndSaveableAsync(int length)
        {
            var expectedBytes = CreateBytes(length);
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
                new DicomOtherByte(DicomTag.PixelData, expectedBytes),
            };
            var file = new DicomFile(dataset);
            var engine = CreateEngine();

            var output = engine.AnonymizeFile(file);

            Assert.Equal(expectedBytes, output.Dataset.GetDicomItem<DicomOtherByte>(DicomTag.PixelData).Get<byte[]>());
            using var stream = new MemoryStream();
            output.Save(stream);
            stream.Position = 0;
            var reopened = DicomFile.Open(stream);
            Assert.Equal(expectedBytes, reopened.Dataset.GetDicomItem<DicomOtherByte>(DicomTag.PixelData).Get<byte[]>());

            using var asyncStream = new MemoryStream();
            await output.SaveAsync(asyncStream);
            asyncStream.Position = 0;
            var asynchronouslyReopened = DicomFile.Open(asyncStream);
            Assert.Equal(expectedBytes, asynchronouslyReopened.Dataset.GetDicomItem<DicomOtherByte>(DicomTag.PixelData).Get<byte[]>());
        }

        [Fact]
        public void GivenKeptNestedLargeBinaryValue_WhenAnonymizing_FileRemainsReadableAndSaveable()
        {
            var expectedBytes = CreateBytes(64 * 1024);
            var nestedDataset = new DicomDataset
            {
                new DicomOtherByte(DicomTag.PixelData, expectedBytes),
            };
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nestedDataset),
            };
            var file = new DicomFile(dataset);
            var engine = CreateEngine();

            var output = engine.AnonymizeFile(file);

            var outputNested = output.Dataset.GetSequence(DicomTag.RequestAttributesSequence).Items[0];
            Assert.Equal(expectedBytes, outputNested.GetDicomItem<DicomOtherByte>(DicomTag.PixelData).Get<byte[]>());
            using var stream = new MemoryStream();
            output.Save(stream);
            stream.Position = 0;
            var reopenedNested = DicomFile.Open(stream).Dataset.GetSequence(DicomTag.RequestAttributesSequence).Items[0];
            Assert.Equal(expectedBytes, reopenedNested.GetDicomItem<DicomOtherByte>(DicomTag.PixelData).Get<byte[]>());
        }

        [Fact]
        public void GivenCustomFileMeta_WhenAnonymizing_CallerMetadataAndDatasetRemainUnchanged()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
                { DicomTag.PatientID, "SYNTHETIC-ID" },
            };
            var file = new DicomFile(dataset);
            var originalFileMeta = file.FileMetaInfo;
            var implementationClassUid = new DicomUID("2.25.300", "Synthetic implementation", DicomUidType.Unknown);
            file.FileMetaInfo.ImplementationClassUID = implementationClassUid;
            file.FileMetaInfo.ImplementationVersionName = "SYNTH_VERSION";
            file.FileMetaInfo.SourceApplicationEntityTitle = "SYNTHETIC_AE";
            var engine = CreateEngine();

            engine.AnonymizeFile(file);

            Assert.Same(dataset, file.Dataset);
            Assert.Same(originalFileMeta, file.FileMetaInfo);
            Assert.Equal(implementationClassUid, file.FileMetaInfo.ImplementationClassUID);
            Assert.Equal("SYNTH_VERSION", file.FileMetaInfo.ImplementationVersionName);
            Assert.Equal("SYNTHETIC_AE", file.FileMetaInfo.SourceApplicationEntityTitle);
            Assert.Equal("SYNTHETIC-ID", file.Dataset.GetString(DicomTag.PatientID));
        }

        [Fact]
        public void GivenInvalidValueRemovedByPolicy_WhenInputValidationIsDisabled_FileIsCleanedWithoutCallerMutation()
        {
            const string invalidAge = "INVALID";
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
            };
            DicomUtility.DisableAutoValidation(dataset);
            dataset.Add(DicomTag.PatientAge, invalidAge);
            var file = new DicomFile(dataset);
            var configuration = new AnonymizerConfiguration
            {
                RuleContent = new[] { new JObject { ["tag"] = "PatientAge", ["method"] = "remove" } },
                DefaultSettings = new AnonymizerDefaultSettings(),
                CustomSettings = new System.Collections.Generic.Dictionary<string, JObject>(),
            };
            var engine = new AnonymizerEngine(
                new AnonymizerConfigurationManager(configuration),
                new AnonymizerEngineOptions(false, false));

            var output = engine.AnonymizeFile(file);

            Assert.False(output.Dataset.Contains(DicomTag.PatientAge));
            Assert.Equal(invalidAge, file.Dataset.GetString(DicomTag.PatientAge));
        }

        [Fact]
        public void GivenInvalidValueRemovedByPolicy_WhenInputValidationIsEnabled_FileIsRejectedWithoutCallerMutation()
        {
            const string invalidAge = "INVALID";
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
            };
            DicomUtility.DisableAutoValidation(dataset);
            dataset.Add(DicomTag.PatientAge, invalidAge);
            var file = new DicomFile(dataset);
            var configuration = new AnonymizerConfiguration
            {
                RuleContent = new[] { new JObject { ["tag"] = "PatientAge", ["method"] = "remove" } },
                DefaultSettings = new AnonymizerDefaultSettings(),
                CustomSettings = new System.Collections.Generic.Dictionary<string, JObject>(),
            };
            var engine = new AnonymizerEngine(
                new AnonymizerConfigurationManager(configuration),
                new AnonymizerEngineOptions(true, false));

            Assert.Throws<DicomValidationException>(() => engine.AnonymizeFile(file));
            Assert.Equal(invalidAge, file.Dataset.GetString(DicomTag.PatientAge));
        }

        [Fact]
        public void GivenStaleFileMetaIdentity_WhenAnonymizing_FileFailsBeforeMutation()
        {
            var file = new DicomFile(new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.100" },
            });
            file.FileMetaInfo.MediaStorageSOPInstanceUID = new DicomUID("2.25.200", "Synthetic", DicomUidType.SOPInstance);
            var engine = CreateEngine(new JObject { ["tag"] = "SOPInstanceUID", ["method"] = "refreshUID" });

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFile(file));

            Assert.Equal(DicomAnonymizationErrorCode.FileMetaIdentityMismatch, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain("2.25.100", exception.Message);
            Assert.DoesNotContain("2.25.200", exception.Message);
            Assert.Equal("2.25.100", file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.Equal("2.25.200", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenMissingOrMultiValueFileMetaIdentity_WhenAnonymizing_FileFailsWithStableError(bool useMultipleValues)
        {
            const string originalUid = "2.25.100";
            var file = new DicomFile(new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, originalUid },
            });
            DicomUtility.DisableAutoValidation(file.FileMetaInfo);
            if (useMultipleValues)
            {
                file.FileMetaInfo.AddOrUpdate(
                    new DicomUniqueIdentifier(
                        DicomTag.MediaStorageSOPInstanceUID,
                        new DicomUID(originalUid, "Synthetic", DicomUidType.SOPInstance),
                        new DicomUID("2.25.200", "Synthetic", DicomUidType.SOPInstance)));
            }
            else
            {
                file.FileMetaInfo.Remove(DicomTag.MediaStorageSOPInstanceUID);
            }

            var engine = CreateEngine();

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFile(file));

            Assert.Equal(DicomAnonymizationErrorCode.FileMetaIdentityMismatch, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain(originalUid, exception.Message);
            Assert.Equal(originalUid, file.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal(useMultipleValues ? 2 : 0, file.FileMetaInfo.GetDicomItem<DicomElement>(DicomTag.MediaStorageSOPInstanceUID)?.Count ?? 0);
        }

        [Fact]
        public void GivenNestedReferenceUid_WhenAnonymizing_RefreshUidRuleIsAppliedWithoutDestroyingSequence()
        {
            const string originalUid = "2.25.100";
            var nestedDataset = new DicomDataset
            {
                { DicomTag.ReferencedSOPInstanceUID, originalUid },
                { DicomTag.PatientID, "SYNTHETIC-ID" },
            };
            var dataset = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, originalUid },
                new DicomSequence(DicomTag.RequestAttributesSequence, nestedDataset),
            };
            var engine = CreateEngine(
                new JObject { ["tag"] = "SOPInstanceUID", ["method"] = "refreshUID" },
                new JObject { ["tag"] = "ReferencedSOPInstanceUID", ["method"] = "refreshUID" });

            engine.AnonymizeDataset(dataset);

            var outputNestedDataset = dataset.GetSequence(DicomTag.RequestAttributesSequence).Items[0];
            var refreshedUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            Assert.NotEqual(originalUid, refreshedUid);
            Assert.Equal(refreshedUid, outputNestedDataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID));
            Assert.Equal("SYNTHETIC-ID", outputNestedDataset.GetSingleValue<string>(DicomTag.PatientID));
            Assert.Single(dataset.GetSequence(DicomTag.RequestAttributesSequence).Items);
        }

        [Fact]
        public void GivenRootAndNestedMultiValueUids_WhenAnonymizing_ValueMultiplicityAndMappingArePreserved()
        {
            var nestedDataset = new DicomDataset
            {
                { DicomTag.SOPClassesInStudy, "2.25.100", "2.25.200", "2.25.100" },
            };
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassesInStudy, "2.25.100", "2.25.200", "2.25.100" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nestedDataset),
            };
            var engine = CreateEngine(new JObject { ["tag"] = "SOPClassesInStudy", ["method"] = "refreshUID" });

            engine.AnonymizeDataset(dataset);

            var rootValues = dataset.GetValues<string>(DicomTag.SOPClassesInStudy);
            var nestedValues = nestedDataset.GetValues<string>(DicomTag.SOPClassesInStudy);
            Assert.Equal(3, rootValues.Length);
            Assert.Equal(rootValues, nestedValues);
            Assert.Equal(rootValues[0], rootValues[2]);
            Assert.NotEqual(rootValues[0], rootValues[1]);
        }

        [Theory]
        [InlineData("ReferencedSOPInstanceUID")]
        [InlineData("(0008,xxxx)")]
        [InlineData("UI")]
        public void GivenEarlierKeepRule_WhenAnonymizing_NestedExactUidRefreshIsBlockedByFirstMatch(string keepSelector)
        {
            const string originalUid = "2.25.100";
            var nestedDataset = new DicomDataset { { DicomTag.ReferencedSOPInstanceUID, originalUid } };
            var dataset = new DicomDataset
            {
                { DicomTag.ReferencedSOPInstanceUID, originalUid },
                new DicomSequence(DicomTag.RequestAttributesSequence, nestedDataset),
            };
            var engine = CreateEngine(
                new JObject { ["tag"] = keepSelector, ["method"] = "keep" },
                new JObject { ["tag"] = "ReferencedSOPInstanceUID", ["method"] = "refreshUID" });

            engine.AnonymizeDataset(dataset);

            Assert.Equal(originalUid, dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID));
            Assert.Equal(originalUid, nestedDataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID));
        }

        [Fact]
        public void GivenNestedUidAtMaximumSequenceDepth_WhenAnonymizing_ReferenceUidIsRefreshed()
        {
            const string originalUid = "2.25.100";
            var dataset = CreateNestedDataset(64, originalUid);
            var engine = CreateEngine(new JObject { ["tag"] = "ReferencedSOPInstanceUID", ["method"] = "refreshUID" });

            engine.AnonymizeDataset(dataset);

            var deepestDataset = GetDeepestDataset(dataset, 64);
            Assert.NotEqual(originalUid, deepestDataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID));
        }

        [Fact]
        public void GivenSequenceDepthAboveLimit_WhenAnonymizing_FileFailsBeforeCallerMutation()
        {
            const string originalUid = "2.25.100";
            const string staleFileMetaUid = "2.25.200";
            var dataset = CreateNestedDataset(65, originalUid);
            dataset.Add(DicomTag.SOPClassUID, DicomUID.CTImageStorage);
            dataset.Add(DicomTag.SOPInstanceUID, originalUid);
            var file = new DicomFile(dataset);
            file.FileMetaInfo.MediaStorageSOPInstanceUID = new DicomUID(staleFileMetaUid, "Synthetic", DicomUidType.SOPInstance);
            var engine = CreateEngine(new JObject { ["tag"] = "SOPInstanceUID", ["method"] = "refreshUID" });

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFile(file));

            Assert.Equal(DicomAnonymizationErrorCode.SequenceDepthLimitExceeded, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain(originalUid, exception.Message);
            Assert.DoesNotContain(staleFileMetaUid, exception.Message);
            Assert.Equal(originalUid, file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.Equal(staleFileMetaUid, file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(originalUid, GetDeepestDataset(file.Dataset, 65).GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID));
        }

        [Fact]
        public void GivenOverDepthDatasetWithInvalidValue_WhenValidatingInput_DepthFailsFirst()
        {
            var dataset = CreateNestedDataset(65, "2.25.100");
            dataset.Add(DicomTag.PatientSex, "INVALID");
            var engine = new AnonymizerEngine(
                new AnonymizerConfigurationManager(new AnonymizerConfiguration
                {
                    RuleContent = System.Array.Empty<JObject>(),
                    DefaultSettings = new AnonymizerDefaultSettings(),
                    CustomSettings = new System.Collections.Generic.Dictionary<string, JObject>(),
                }),
                new AnonymizerEngineOptions(true, false));

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.SequenceDepthLimitExceeded, exception.DicomAnonymizerErrorCode);
            Assert.Equal("INVALID", dataset.GetString(DicomTag.PatientSex));
            Assert.Equal("2.25.100", GetDeepestDataset(dataset, 65).GetString(DicomTag.ReferencedSOPInstanceUID));
        }

        [Theory]
        [InlineData("remove")]
        [InlineData("redact")]
        public void GivenRequiredSopInstanceUidIsRemovedOrCleared_WhenAnonymizing_FileFailsWithoutCallerMutation(string method)
        {
            const string originalUid = "2.25.100";
            var file = new DicomFile(new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, originalUid },
            });
            var rule = new JObject { ["tag"] = "SOPInstanceUID", ["method"] = method };
            if (method == "redact")
            {
                rule["params"] = new JObject();
            }

            var engine = CreateEngine(rule);

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFile(file));

            Assert.Equal(DicomAnonymizationErrorCode.FileMetaIdentityMismatch, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain(originalUid, exception.Message);
            Assert.Equal(originalUid, file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.Equal(originalUid, file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
        }

        [Fact]
        public void GivenInvariantUidRule_WhenAnonymizing_RuleIsRejectedBeforeMutation()
        {
            var dataset = new DicomDataset { { DicomTag.SOPClassUID, DicomUID.CTImageStorage } };

            Assert.Throws<AnonymizerConfigurationException>(() =>
                CreateEngine(new JObject { ["tag"] = "SOPClassUID", ["method"] = "refreshUID" }));
            Assert.Equal(DicomUID.CTImageStorage, dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID));
        }

        [Theory]
        [InlineData("UI")]
        [InlineData("(0008,xxxx)")]
        public void GivenSelectorThatCanMutateInvariantUid_WhenCreatingEngine_ConfigurationIsRejected(string selector)
        {
            var json = $"{{\"rules\":[{{\"tag\":\"{selector}\",\"method\":\"refreshUID\"}}],\"defaultSettings\":{{}},\"customSettings\":{{}}}}";
            Assert.Throws<AnonymizerConfigurationException>(() => new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json)));
        }

        private static DicomDataset CreateNestedDataset(int depth, string referencedUid)
        {
            var root = new DicomDataset();
            var current = root;
            for (var index = 0; index < depth; index++)
            {
                var nested = new DicomDataset();
                current.Add(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
                current = nested;
            }

            current.Add(DicomTag.ReferencedSOPInstanceUID, referencedUid);
            return root;
        }

        private static DicomDataset GetDeepestDataset(DicomDataset dataset, int depth)
        {
            var current = dataset;
            for (var index = 0; index < depth; index++)
            {
                current = current.GetSequence(DicomTag.RequestAttributesSequence).Items[0];
            }

            return current;
        }

        private static byte[] CreateBytes(int length)
        {
            return Enumerable.Range(0, length).Select(index => (byte)(index % 251)).ToArray();
        }

        private static AnonymizerEngine CreateEngine(params JObject[] rules)
        {
            var configuration = new AnonymizerConfiguration
            {
                RuleContent = rules,
                DefaultSettings = new AnonymizerDefaultSettings(),
                CustomSettings = new System.Collections.Generic.Dictionary<string, JObject>(),
            };
            return new AnonymizerEngine(new AnonymizerConfigurationManager(configuration));
        }
    }
}
