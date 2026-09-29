// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.CommandLineTool;
using Microsoft.Health.Dicom.Anonymizer.Core;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerToolUnitTests
    {
        private const string EmbeddedPdfStorageUid = "1.2.840.10008.5.1.4.1.1.104.1";
        private const string EmbeddedCdaStorageUid = "1.2.840.10008.5.1.4.1.1.104.2";
        private const string PlantedIdentifier = "Patient: Embedded Payload Person; MRN: 8675309";

        public static IEnumerable<object[]> GetInvalidCommandLine()
        {
            yield return new object[] { "-i DicomFiles/I341.dcm" };
            yield return new object[] { "-o DicomFiles/I341.dcm" };
            yield return new object[] { "-I DicomFiles/I341.dcm" };
            yield return new object[] { "-O DicomFiles/I341.dcm" };
            yield return new object[] { "-i DicomFiles/I341.dcm -o DicomFiles/I341.dcm" };
            yield return new object[] { "-I DicomFiles -O DicomFiles" };
        }

        [Fact]
        public async Task GivenOneDicomFile_WhenAnonymize_ResultWillBeReturnedAsync()
        {
            var commands = "-i DicomFiles/I341.dcm -o I341.dcm";
            await AnonymizerCliTool.Main(commands.Split());
            var dicomFile = await DicomFile.OpenAsync("I341.dcm");
            var expectedDicomFile = await DicomFile.OpenAsync("DicomResults/I341.dcm");
            Assert.Equal(expectedDicomFile.Dataset, dicomFile.Dataset);
            File.Delete("I341.dcm");
        }

        [Fact]
        public async Task GivenOneDicomFile_WhenAnonymizeWithUnsupportedActualItem_OperationIsRejectedAsync()
        {
            var engine = new AnonymizerEngine("TestConfigs/invalidOutputConfig.json");
            Assert.NotNull(engine);
            var commands = "-i DicomFiles/I341.dcm -o I341-invalid.dcm -c TestConfigs/invalidOutputConfig.json";
            var error = await Assert.ThrowsAsync<AnonymizerOperationException>(async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.False(File.Exists("I341-invalid.dcm"));
        }

        [Fact]
        public async Task GivenOneDicomFile_WhenBroadRuleSelectsInvariantUid_InputWillBeRejectedAsync()
        {
            const string outputFile = "I341-newConfig.dcm";
            var commands = $"-i DicomFiles/I341.dcm -o {outputFile} -c TestConfigs/newConfig.json";

            var engine = new AnonymizerEngine("TestConfigs/newConfig.json");
            Assert.NotNull(engine);

            var error = await Assert.ThrowsAsync<AnonymizerOperationException>(
                async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
            Assert.False(File.Exists(outputFile));
        }

        [Fact]
        public async Task GivenStaleFileMetaIdentity_WhenAnonymize_NoPartialOutputWillBeWrittenAsync()
        {
            var inputFile = $"stale-{Guid.NewGuid():N}.dcm";
            var outputFile = $"stale-output-{Guid.NewGuid():N}.dcm";
            var dicomFile = await DicomFile.OpenAsync("DicomFiles/I341.dcm");
            var originalUid = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID);
            const string staleUid = "2.25.999";
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = new DicomUID(staleUid, "Synthetic", DicomUidType.SOPInstance);
            dicomFile.Save(inputFile);

            try
            {
                var commands = $"-i {inputFile} -o {outputFile}";
                var exception = await Assert.ThrowsAsync<AnonymizerOperationException>(
                    async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

                Assert.Equal(DicomAnonymizationErrorCode.FileMetaIdentityMismatch, exception.DicomAnonymizerErrorCode);
                Assert.DoesNotContain(originalUid, exception.Message);
                Assert.DoesNotContain(staleUid, exception.Message);
                Assert.False(File.Exists(outputFile));
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        [Fact]
        public async Task GivenSequenceDepthAboveLimit_WhenAnonymize_NoPartialOutputWillBeWrittenAsync()
        {
            var inputFile = $"deep-{Guid.NewGuid():N}.dcm";
            var outputFile = $"deep-output-{Guid.NewGuid():N}.dcm";
            var dicomFile = await DicomFile.OpenAsync("DicomFiles/I341.dcm");
            var current = dicomFile.Dataset;
            for (var index = 0; index < 65; index++)
            {
                var nested = new DicomDataset();
                current.Add(new DicomSequence(DicomTag.RequestAttributesSequence, nested));
                current = nested;
            }

            current.Add(DicomTag.ReferencedSOPInstanceUID, "2.25.999");
            dicomFile.Save(inputFile);

            try
            {
                var commands = $"-i {inputFile} -o {outputFile}";
                var exception = await Assert.ThrowsAsync<AnonymizerOperationException>(
                    async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

                Assert.Equal(DicomAnonymizationErrorCode.SequenceDepthLimitExceeded, exception.DicomAnonymizerErrorCode);
                Assert.DoesNotContain("2.25.999", exception.Message);
                Assert.DoesNotContain(inputFile, exception.Message);
                Assert.DoesNotContain(outputFile, exception.Message);
                Assert.False(File.Exists(outputFile));
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        [Theory]
        [InlineData("remove")]
        [InlineData("redact")]
        public async Task GivenRequiredSopInstanceUidIsRemovedOrCleared_WhenAnonymize_NoPartialOutputWillBeWrittenAsync(string method)
        {
            var inputFile = $"required-identity-{Guid.NewGuid():N}.dcm";
            var outputFile = $"required-identity-output-{Guid.NewGuid():N}.dcm";
            var configFile = $"required-identity-config-{Guid.NewGuid():N}.json";
            var dicomFile = await DicomFile.OpenAsync("DicomFiles/I341.dcm");
            var originalUid = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID);
            dicomFile.Save(inputFile);
            await File.WriteAllTextAsync(
                configFile,
                $"{{\"rules\":[{{\"tag\":\"SOPInstanceUID\",\"method\":\"{method}\",\"params\":{{}}}}],\"defaultSettings\":{{}},\"customSettings\":{{}}}}");

            try
            {
                var commands = $"-i {inputFile} -o {outputFile} -c {configFile}";
                var exception = await Assert.ThrowsAsync<AnonymizerOperationException>(
                    async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

                Assert.Equal(DicomAnonymizationErrorCode.FileMetaIdentityMismatch, exception.DicomAnonymizerErrorCode);
                Assert.DoesNotContain(originalUid, exception.Message);
                Assert.DoesNotContain(inputFile, exception.Message);
                Assert.DoesNotContain(outputFile, exception.Message);
                Assert.False(File.Exists(outputFile));
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
                File.Delete(configFile);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GivenUnsupportedNestedAction_WhenAnonymize_SourceAndDestinationArePreservedAsync(bool existingDestination)
        {
            var inputFile = $"nested-input-{Guid.NewGuid():N}.dcm";
            var outputFile = $"nested-output-{Guid.NewGuid():N}.dcm";
            var configFile = $"nested-config-{Guid.NewGuid():N}.json";
            var destinationBytes = Encoding.UTF8.GetBytes("EXISTING DESTINATION");
            var file = await DicomFile.OpenAsync("DicomFiles/I341.dcm");
            file.Dataset.AddOrUpdate(new DicomSequence(
                DicomTag.RequestAttributesSequence,
                new DicomDataset { { DicomTag.StudyDate, "20250509" } }));
            file.Save(inputFile);
            var inputBytes = await File.ReadAllBytesAsync(inputFile);
            await File.WriteAllTextAsync(
                configFile,
                "{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"cryptoHash\"},{\"tag\":\"StudyDate\",\"method\":\"dateShift\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"synthetic-key\"},\"dateshift\":{\"dateShiftKey\":\"synthetic-key\"}}}");
            if (existingDestination)
            {
                await File.WriteAllBytesAsync(outputFile, destinationBytes);
            }

            try
            {
                var error = await Assert.ThrowsAsync<AnonymizerOperationException>(() =>
                    AnonymizerCliTool.ExecuteCommandsAsync(new[] { "-i", inputFile, "-o", outputFile, "-c", configFile }));

                Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, error.DicomAnonymizerErrorCode);
                Assert.DoesNotContain("20250509", error.Message);
                Assert.DoesNotContain(inputFile, error.Message);
                Assert.Equal(inputBytes, await File.ReadAllBytesAsync(inputFile));
                if (existingDestination)
                {
                    Assert.Equal(destinationBytes, await File.ReadAllBytesAsync(outputFile));
                }
                else
                {
                    Assert.False(File.Exists(outputFile));
                }
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
                File.Delete(configFile);
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidCommandLine))]
        public async Task GivenOneDicomFile_WhenAnonymizeWithInvalidCommandLine_ExceptionWillBeThrownAsync(string commands)
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));
        }

        [Fact]
        public async Task GivenDicomFolder_WhenAnonymize_ResultWillBeWrittenInOutputFolderAsync()
        {
            var commands = "-I DicomFiles -O Output";
            await AnonymizerCliTool.Main(commands.Split());

            foreach (string file in Directory.EnumerateFiles("Output", "*.dcm", SearchOption.AllDirectories))
            {
                Assert.Equal(DicomFile.Open(file).Dataset, DicomFile.Open(Path.Combine("DicomResults", Path.GetFileName(file))).Dataset);
            }

            Directory.Delete("Output", true);
        }

        [Fact]
        public async Task GivenOneDicomFileWithPrivateTags_WhenAnonymize_PrivateTagsWillBePresentAsync()
        {
            string fileName = "privateTag.dcm";
            var commands = $"-i DicomFiles/{fileName} -o {fileName}";
            await AnonymizerCliTool.Main(commands.Split());
            var dicomFile = await DicomFile.OpenAsync(fileName);
            var expectedDicomFile = await DicomFile.OpenAsync("DicomResults/" + fileName);
            Assert.Equal(expectedDicomFile.Dataset, dicomFile.Dataset);
            File.Delete(fileName);
        }

        [Fact]
        public async Task GivenOneDicomFileWithPrivateTagsAndConfigSet_WhenAnonymize_PrivateTagsWillBeRemovedAsync()
        {
            string fileName = "privateTag.dcm";
            string outputFileName = "privateTagRemoved.dcm";
            var commands = $"-i DicomFiles/{fileName} -o {outputFileName} -c TestConfigs/privateTagConfig.json";
            await AnonymizerCliTool.Main(commands.Split());
            var dicomFile = await DicomFile.OpenAsync(outputFileName);
            var expectedDicomFile = await DicomFile.OpenAsync("DicomResults/" + outputFileName);
            Assert.Equal(expectedDicomFile.Dataset, dicomFile.Dataset);
            File.Delete(outputFileName);
        }

        [Theory]
        [InlineData(EmbeddedPdfStorageUid, "%PDF-1.7\nPatient: Embedded Payload Person; MRN: 8675309")]
        [InlineData(EmbeddedCdaStorageUid, "<ClinicalDocument><patient>Embedded Payload Person</patient><id>8675309</id></ClinicalDocument>")]
        public async Task GivenEmbeddedPayload_WhenAnonymize_NoOutputIsCreatedAsync(string sopClassUid, string payload)
        {
            string testDirectory = Path.GetRandomFileName();
            string inputFileName = Path.Combine(testDirectory, "input.dcm");
            string outputFileName = Path.Combine(testDirectory, "output.dcm");

            try
            {
                Directory.CreateDirectory(testDirectory);
                await CreateEmbeddedPayloadFileAsync(inputFileName, sopClassUid, payload);
                string commands = $"-i {inputFileName} -o {outputFileName}";

                AnonymizerOperationException exception = await Assert.ThrowsAsync<AnonymizerOperationException>(
                    async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

                Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
                Assert.DoesNotContain("Embedded Payload Person", exception.Message);
                Assert.DoesNotContain("8675309", exception.Message);
                Assert.DoesNotContain(inputFileName, exception.Message);
                Assert.DoesNotContain(outputFileName, exception.Message);
                Assert.False(File.Exists(outputFileName));
                Assert.Equal(new[] { inputFileName }, Directory.GetFiles(testDirectory));
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        [Fact]
        public async Task GivenEmbeddedPayloadAndExistingDestination_WhenAnonymize_DestinationIsPreservedAsync()
        {
            string testDirectory = Path.GetRandomFileName();
            string inputFileName = Path.Combine(testDirectory, "input.dcm");
            string outputFileName = Path.Combine(testDirectory, "output.dcm");
            byte[] existingContent = Encoding.UTF8.GetBytes("existing destination");

            try
            {
                Directory.CreateDirectory(testDirectory);
                await CreateEmbeddedPayloadFileAsync(inputFileName, EmbeddedPdfStorageUid, $"%PDF-1.7\n{PlantedIdentifier}");
                await File.WriteAllBytesAsync(outputFileName, existingContent);
                string commands = $"-i {inputFileName} -o {outputFileName}";

                AnonymizerOperationException exception = await Assert.ThrowsAsync<AnonymizerOperationException>(
                    async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

                Assert.DoesNotContain("Embedded Payload Person", exception.Message);
                Assert.DoesNotContain("8675309", exception.Message);
                Assert.DoesNotContain(inputFileName, exception.Message);
                Assert.DoesNotContain(outputFileName, exception.Message);
                Assert.Equal(existingContent, await File.ReadAllBytesAsync(outputFileName));
                Assert.Equal(2, Directory.GetFiles(testDirectory).Length);
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        [Fact]
        public async Task GivenNestedEmbeddedPayloadAndExistingDestination_WhenAnonymize_NoOutputResidueIsCreatedAsync()
        {
            string testDirectory = Path.GetRandomFileName();
            string inputFileName = Path.Combine(testDirectory, "input.dcm");
            string outputFileName = Path.Combine(testDirectory, "output.dcm");
            byte[] existingContent = Encoding.UTF8.GetBytes("existing destination");

            try
            {
                Directory.CreateDirectory(testDirectory);
                await CreateNestedEmbeddedPayloadFileAsync(inputFileName);
                byte[] inputContent = await File.ReadAllBytesAsync(inputFileName);
                await File.WriteAllBytesAsync(outputFileName, existingContent);
                string commands = $"-i {inputFileName} -o {outputFileName}";

                AnonymizerOperationException exception = await Assert.ThrowsAsync<AnonymizerOperationException>(
                    async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

                Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
                Assert.Equal(
                    "Unsupported embedded payload rejected. ErrorCode=1201; PayloadTypes=EncapsulatedDocument; DetectedTags=(0042,0011).",
                    exception.Message);
                Assert.DoesNotContain("Embedded Payload Person", exception.Message);
                Assert.DoesNotContain("8675309", exception.Message);
                Assert.DoesNotContain(inputFileName, exception.Message);
                Assert.DoesNotContain(outputFileName, exception.Message);
                Assert.Equal(inputContent, await File.ReadAllBytesAsync(inputFileName));
                Assert.Equal(existingContent, await File.ReadAllBytesAsync(outputFileName));
                Assert.Equal(2, Directory.GetFiles(testDirectory).Length);
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        private static async Task CreateEmbeddedPayloadFileAsync(string fileName, string sopClassUid, string payload)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, sopClassUid },
                { DicomTag.SOPInstanceUID, "2.25.322924430372144810477559413499190252923" },
                { DicomTag.EncapsulatedDocument, Encoding.UTF8.GetBytes(payload) },
            };

            await new DicomFile(dataset).SaveAsync(fileName);
        }

        private static async Task CreateNestedEmbeddedPayloadFileAsync(string fileName)
        {
            var nestedDataset = new DicomDataset
            {
                { DicomTag.EncapsulatedDocument, Encoding.UTF8.GetBytes(PlantedIdentifier) },
            };
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, "1.2.3.4" },
                { DicomTag.SOPInstanceUID, "2.25.322924430372144810477559413499190252923" },
                new DicomSequence(DicomTag.ContentSequence, nestedDataset),
            };

            var file = new DicomFile(dataset);
            file.FileMetaInfo.MediaStorageSOPInstanceUID = new DicomUID("2.25.200", "Synthetic", DicomUidType.SOPInstance);
            await file.SaveAsync(fileName);
        }
    }
}
