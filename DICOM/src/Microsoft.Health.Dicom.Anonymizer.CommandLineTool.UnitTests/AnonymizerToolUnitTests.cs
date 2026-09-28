// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.CommandLineTool;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerToolUnitTests
    {
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
        public async Task GivenOneDicomFile_WhenAnonymizeWithUnsafeBroadConfig_ConfigurationWillBeRejectedAsync()
        {
            const string outputFile = "I341-newConfig.dcm";
            var commands = $"-i DicomFiles/I341.dcm -o {outputFile} -c TestConfigs/newConfig.json";

            await Assert.ThrowsAsync<AnonymizerConfigurationException>(
                async () => await AnonymizerCliTool.ExecuteCommandsAsync(commands.Split()));

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
    }
}
