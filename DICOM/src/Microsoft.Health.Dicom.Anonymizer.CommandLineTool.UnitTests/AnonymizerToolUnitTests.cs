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
            Assert.True(CompareDicomFileDataSet(expectedDicomFile.Dataset, dicomFile.Dataset));
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
            Assert.True(CompareDicomFileDataSet(expectedDicomFile.Dataset, dicomFile.Dataset));
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
                Assert.True(CompareDicomFileDataSet(
                    DicomFile.Open(file).Dataset,
                    DicomFile.Open(Path.Combine("DicomResults", Path.GetFileName(file))).Dataset));
            }

            Directory.Delete("Output", true);
        }

        private bool CompareDicomFileDataSet(DicomDataset dcm1, DicomDataset dcm2)
        {
            foreach (var item in dcm1)
            {
                if (item.ValueRepresentation != DicomVR.UI && item is DicomElement)
                {
                    if (!string.Equals(
                        dcm1.GetDicomItem<DicomElement>(item.Tag).Get<string>(),
                        dcm2.GetDicomItem<DicomElement>(item.Tag).Get<string>()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
