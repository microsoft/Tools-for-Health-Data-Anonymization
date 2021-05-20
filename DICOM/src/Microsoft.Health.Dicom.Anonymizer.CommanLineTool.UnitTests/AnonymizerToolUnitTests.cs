// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Tool;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerToolUnitTests
    {
        [Fact]
        public async Task GivenOneDicomFile_WhenAnonymize_ResultWillBeReturnedAsync()
        {
            var commands = "-i DicomFiles/I341.dcm -o I341.dcm";
            await AnonymizerCliTool.Main(commands.Split());
            var dicomFile = DicomFile.Open("I341.dcm");
            var expectedDicomFile = DicomFile.Open("DicomResults/I341.dcm");
            Assert.True(CompareDicomFileDataSet(expectedDicomFile.Dataset, dicomFile.Dataset));
            File.Delete("I341.dcm");
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
