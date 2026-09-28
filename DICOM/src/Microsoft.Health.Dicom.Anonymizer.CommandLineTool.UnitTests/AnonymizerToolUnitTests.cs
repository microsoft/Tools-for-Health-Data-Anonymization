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
        public async Task GivenOneDicomFile_WhenAnonymizeWithInvalidOutput_IfValidateOutput_ExceptionWillBeThrownAsync()
        {
            var commands = "-i DicomFiles/I341.dcm -o I341-invalid.dcm -c TestConfigs/invalidOutputConfig.json";
            await Assert.ThrowsAsync<AnonymizerOperationException>(async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));
        }

        [Fact]
        public async Task GivenOneDicomFile_WhenAnonymizeWithNewConfig_ResultWillBeReturnedAsync()
        {
            var commands = "-i DicomFiles/I341.dcm -o I341-newConfig.dcm -c TestConfigs/newConfig.json";
            await AnonymizerCliTool.Main(commands.Split());
            var dicomFile = await DicomFile.OpenAsync("I341-newConfig.dcm");
            var expectedDicomFile = await DicomFile.OpenAsync("DicomResults/I341-newConfig.dcm");
            Assert.Equal(expectedDicomFile.Dataset, dicomFile.Dataset);
            File.Delete("I341-newConfig.dcm");
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

            await new DicomFile(dataset).SaveAsync(fileName);
        }
    }
}
