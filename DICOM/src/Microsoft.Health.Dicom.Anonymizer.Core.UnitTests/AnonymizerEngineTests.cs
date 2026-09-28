// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
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
            Assert.Throws<AnonymizerConfigurationException>(
                () => new AnonymizerEngine("./TestConfigurations/configuration-invalid-string-output.json", new AnonymizerEngineOptions(validateOutput: true)));
        }

        [Fact]
        public void GivenInvalidDicomDataSet_SetValidateInput_ExceptionWillBeThrown()
        {
            DicomUtility.DisableAutoValidation(Dataset);
            Dataset.AddOrUpdate(DicomTag.PatientAge, "invalid");
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json", new AnonymizerEngineOptions(validateInput: true));
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
            Assert.Throws<AnonymizerConfigurationException>(
                () => new AnonymizerEngine("./TestConfigurations/configuration-invalid-string-output.json", new AnonymizerEngineOptions(false, false)));
        }

        [Theory]
        [InlineData("{\"rules\":[{\"tag\":\"PatientWeight\",\"method\":\"keep\"},{\"tag\":\"(0010,1030)\",\"method\":\"remove\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"UI\",\"method\":\"refreshUID\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"SQ\",\"method\":\"remove\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"StudyDate\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"(0008,00xx)\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"keep\",\"unexpected\":true}]}")]
        public void GivenUnsafeOrAmbiguousPolicy_WhenConstructingEngine_FailsBeforeDatasetMutation(string json)
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(json);

            var exception = Assert.Throws<AnonymizerConfigurationException>(() => new AnonymizerEngine(manager));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidConfigurationValues, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain("PatientWeight", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("StudyDate", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void GivenMaskedCryptoHashRule_WhenConstructingEngine_FailsBeforeDatasetMutation()
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"(0008,00xx)\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}");
            var dataset = new DicomDataset { { DicomTag.StudyDate, "20240101" } };
            var originalValue = dataset.GetString(DicomTag.StudyDate);

            Assert.Throws<AnonymizerConfigurationException>(() => new AnonymizerEngine(manager));
            Assert.Equal(originalValue, dataset.GetString(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenUnknownTopLevelField_WhenParsingPolicy_FailsClosed()
        {
            var exception = Assert.Throws<AnonymizerConfigurationException>(
                () => AnonymizerConfigurationManager.CreateFromJson("{\"rules\":[],\"unexpected\":true}"));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidConfigurationValues, exception.DicomAnonymizerErrorCode);
        }

        [Theory]
        [InlineData("{\"rules\":[],\"rules\":[]}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"tag\":\"PatientID\",\"method\":\"keep\"}]}")]
        public void GivenExactDuplicateJsonField_WhenParsingPolicy_ReturnsParsingConfigurationError(string json)
        {
            var exception = Assert.Throws<AnonymizerConfigurationException>(
                () => AnonymizerConfigurationManager.CreateFromJson(json));

            Assert.Equal(DicomAnonymizationErrorCode.ParsingJsonConfigurationFailed, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain("PatientName", exception.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("PatientID", exception.ToString(), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("{\"rules\":[],\"Rules\":[]}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"Tag\":\"PatientID\",\"method\":\"keep\"}]}")]
        public void GivenCaseVariantDuplicateJsonField_WhenConstructingEngine_FailsBeforeDatasetMutation(string json)
        {
            var dataset = new DicomDataset { { DicomTag.PatientName, "PRIVATE" } };
            var originalValue = dataset.GetString(DicomTag.PatientName);

            var exception = Assert.Throws<AnonymizerConfigurationException>(
                () => new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json)));

            Assert.Equal(DicomAnonymizationErrorCode.InvalidConfigurationValues, exception.DicomAnonymizerErrorCode);
            Assert.Equal(originalValue, dataset.GetString(DicomTag.PatientName));
            Assert.DoesNotContain("PatientName", exception.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("PatientID", exception.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void GivenUnambiguousSpecificAndBroadRules_WhenConstructingEngine_PreservesRulePrecedence()
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"keep\"},{\"tag\":\"PN\",\"method\":\"redact\"}],\"defaultSettings\":{\"redact\":{}}}");
            var dataset = new DicomDataset { { DicomTag.PatientName, "PRIVATE" }, { DicomTag.ReferringPhysicianName, "REMOVE" } };
            var engine = new AnonymizerEngine(manager);

            engine.AnonymizeDataset(dataset);

            Assert.Equal("PRIVATE", dataset.GetString(DicomTag.PatientName));
            Assert.Null(dataset.GetString(DicomTag.ReferringPhysicianName));
        }

        [Fact]
        public void GivenSharedDefaultSettings_WhenRuleParametersAreMerged_SettingsDoNotBleedBetweenRules()
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"cryptoHash\",\"params\":{\"matchInputStringLength\":true}},{\"tag\":\"IssuerOfPatientID\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\",\"matchInputStringLength\":false}}}");
            var dataset = new DicomDataset { { DicomTag.PatientID, "ABCD" }, { DicomTag.IssuerOfPatientID, "EFGH" } };
            var engine = new AnonymizerEngine(manager);

            engine.AnonymizeDataset(dataset);

            Assert.Equal(4, dataset.GetString(DicomTag.PatientID).Length);
            Assert.Equal(64, dataset.GetString(DicomTag.IssuerOfPatientID).Length);
        }

        [Fact]
        public void GivenSpecificPrivateRuleBeforeMaskedRule_WhenAnonymizing_SpecificRuleKeepsPrecedence()
        {
            var privateTag = new DicomTag(0x0011, 0x1010);
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"(0011,1010)\",\"method\":\"keep\"},{\"tag\":\"(0011,10xx)\",\"method\":\"redact\"}],\"defaultSettings\":{\"redact\":{}}}");
            var dataset = new DicomDataset();
            dataset.AddOrUpdate(DicomVR.LO, privateTag, "PRIVATE");
            var engine = new AnonymizerEngine(manager);

            engine.AnonymizeDataset(dataset);

            Assert.Equal("PRIVATE", dataset.GetString(privateTag));
        }
    }
}
