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

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task GivenInvalidMetadata_WhenOptionalValidationFails_DiagnosticsAreValueFreeAndDestinationIsUntouchedAsync(bool validateInput, bool existingDestination)
        {
            const string canary = "SYNTHETIC-CLI-VALIDATION-PHI-CANARY";
            var directory = Path.Combine(Path.GetTempPath(), "dicom-validation-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var input = Path.Combine(directory, "input.dcm");
            var output = Path.Combine(directory, "output.dcm");
            var config = Path.Combine(directory, "config.json");
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            try
            {
                var dataset = new DicomDataset
                {
                    { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                    { DicomTag.SOPInstanceUID, "2.25.123" },
                    { DicomTag.PatientName, "SYNTHETIC^NAME" },
                };
                DicomUtility.DisableAutoValidation(dataset);
                dataset.Add(DicomTag.InstitutionName, canary + new string('Z', 65));
                new DicomFile(dataset).Save(input);
                var originalBytes = await File.ReadAllBytesAsync(input);
                await File.WriteAllTextAsync(config, "{'rules':[{'tag':'PatientName','method':'remove'}]}");
                var destinationBytes = Encoding.ASCII.GetBytes("EXISTING DESTINATION");
                if (existingDestination)
                {
                    await File.WriteAllBytesAsync(output, destinationBytes);
                }

                var args = new[] { "-i", input, "-o", output, "-c", config, validateInput ? "--validateInput" : "--validateOutput" };
                var error = await Assert.ThrowsAsync<AnonymizerOperationException>(() => AnonymizerCliTool.ExecuteCommandsAsync(args));
                var expectedCode = validateInput ? DicomAnonymizationErrorCode.InputDatasetValidationFailed : DicomAnonymizationErrorCode.OutputDatasetValidationFailed;
                Assert.Equal(expectedCode, error.DicomAnonymizerErrorCode);
                Assert.Null(error.InnerException);
                Assert.DoesNotContain(canary, error.ToString());

                Console.SetOut(stdout);
                Console.SetError(stderr);
                Assert.Equal(-1, await AnonymizerCliTool.Main(args));

                Assert.Empty(stdout.ToString());
                Assert.Equal($"Process failed with error code {(int)expectedCode}: DICOM dataset validation failed.{Environment.NewLine}", stderr.ToString());
                Assert.DoesNotContain(canary, stdout.ToString() + stderr.ToString());
                Assert.Equal(originalBytes, await File.ReadAllBytesAsync(input));
                if (existingDestination)
                {
                    Assert.Equal(destinationBytes, await File.ReadAllBytesAsync(output));
                }
                else
                {
                    Assert.False(File.Exists(output));
                }
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                Directory.Delete(directory, recursive: true);
            }
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GivenOptionalValidationControls_WhenAnonymizing_DefaultsAndEmptyValuesRemainSupportedAsync(bool emptyValue)
        {
            var directory = Path.Combine(Path.GetTempPath(), "dicom-validation-control-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var input = Path.Combine(directory, "input.dcm");
                var output = Path.Combine(directory, "output.dcm");
                var config = Path.Combine(directory, "config.json");
                var value = emptyValue ? string.Empty : new string('Z', 65);
                var dataset = new DicomDataset
                {
                    { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                    { DicomTag.SOPInstanceUID, "2.25.123" },
                    { DicomTag.PatientName, "SYNTHETIC^NAME" },
                };
                DicomUtility.DisableAutoValidation(dataset);
                dataset.Add(DicomTag.InstitutionName, value);
                new DicomFile(dataset).Save(input);
                await File.WriteAllTextAsync(config, "{'rules':[{'tag':'PatientName','method':'remove'}]}");
                var args = new List<string> { "-i", input, "-o", output, "-c", config };
                if (emptyValue)
                {
                    args.Add("--validateInput");
                    args.Add("--validateOutput");
                }

                Assert.Equal(0, await AnonymizerCliTool.Main(args.ToArray()));

                var result = DicomFile.Open(output, FileReadOption.ReadAll);
                Assert.False(result.Dataset.Contains(DicomTag.PatientName));
                Assert.Equal(value, result.Dataset.GetString(DicomTag.InstitutionName) ?? string.Empty);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
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
