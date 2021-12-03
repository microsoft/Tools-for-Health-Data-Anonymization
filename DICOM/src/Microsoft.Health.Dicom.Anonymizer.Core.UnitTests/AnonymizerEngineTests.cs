// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
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
    }
}
