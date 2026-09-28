// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        private const int NestedSequenceDepth = 128;
        private const string PlantedIdentifier = "Patient: Embedded Payload Person; MRN: 8675309";

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

        [Theory]
        [InlineData("1.2.840.10008.5.1.4.1.1.104.1", "%PDF-1.7\nPatient: Embedded Payload Person; MRN: 8675309")]
        [InlineData("1.2.840.10008.5.1.4.1.1.104.2", "<ClinicalDocument><patient>Embedded Payload Person</patient><id>8675309</id></ClinicalDocument>")]
        public void GivenEmbeddedPayload_WhenAnonymize_PayloadIsRejectedBeforeMutation(string sopClassUid, string payload)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, sopClassUid },
                { DicomTag.SOPInstanceUID, "2.25.322924430372144810477559413499190252923" },
                { DicomTag.PatientName, "Metadata^Identifier" },
                { DicomTag.EncapsulatedDocument, Encoding.UTF8.GetBytes(payload) },
            };
            byte[] originalPayload = dataset.GetValues<byte>(DicomTag.EncapsulatedDocument);
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json");

            AnonymizerOperationException exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
            Assert.Equal(
                "Unsupported embedded payload rejected. ErrorCode=1201; PayloadTypes=EncapsulatedDocument; DetectedTags=(0042,0011).",
                exception.Message);
            Assert.DoesNotContain("Embedded Payload Person", exception.Message);
            Assert.DoesNotContain("8675309", exception.Message);
            Assert.DoesNotContain("Metadata^Identifier", exception.Message);
            Assert.Equal(originalPayload, dataset.GetValues<byte>(DicomTag.EncapsulatedDocument));
            Assert.Equal("Metadata^Identifier", dataset.GetSingleValue<string>(DicomTag.PatientName));
        }

        [Theory]
        [InlineData("1.2.840.10008.5.1.4.1.1.104.1", "EncapsulatedPDF")]
        [InlineData("1.2.840.10008.5.1.4.1.1.104.2", "EncapsulatedCDA")]
        public void GivenEmbeddedDocumentSopClassWithoutPayload_WhenAnonymize_DatasetIsRejected(string sopClassUid, string payloadType)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, sopClassUid },
            };
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json");

            AnonymizerOperationException exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
            Assert.Equal(
                $"Unsupported embedded payload rejected. ErrorCode=1201; PayloadTypes={payloadType}; DetectedTags=(0008,0016).",
                exception.Message);
        }

        [Fact]
        public void GivenEncapsulatedDocumentWithUnexpectedSopClass_WhenAnonymize_PayloadIsRejected()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, "1.2.3.4" },
                { DicomTag.EncapsulatedDocument, Encoding.UTF8.GetBytes(PlantedIdentifier) },
            };
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json");

            AnonymizerOperationException exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
            Assert.Contains("PayloadTypes=EncapsulatedDocument", exception.Message);
            Assert.DoesNotContain(PlantedIdentifier, exception.Message);
        }

        [Fact]
        public void GivenEncapsulatedDocumentWithMultiValuedSopClass_WhenAnonymize_PayloadIsRejectedBeforeSopClassParsing()
        {
            var dataset = new DicomDataset();
            DicomUtility.DisableAutoValidation(dataset);
            dataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.3", "4.5.6");
            dataset.AddOrUpdate(DicomTag.PatientName, "Metadata^Identifier");
            dataset.AddOrUpdate(DicomTag.EncapsulatedDocument, Encoding.UTF8.GetBytes(PlantedIdentifier));
            byte[] originalPayload = dataset.GetValues<byte>(DicomTag.EncapsulatedDocument);
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json");

            AnonymizerOperationException exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
            Assert.Equal(
                "Unsupported embedded payload rejected. ErrorCode=1201; PayloadTypes=EncapsulatedDocument; DetectedTags=(0042,0011).",
                exception.Message);
            Assert.DoesNotContain(PlantedIdentifier, exception.Message);
            Assert.DoesNotContain("Metadata^Identifier", exception.Message);
            Assert.Equal(originalPayload, dataset.GetValues<byte>(DicomTag.EncapsulatedDocument));
            Assert.Equal("Metadata^Identifier", dataset.GetSingleValue<string>(DicomTag.PatientName));
        }

        [Fact]
        public void GivenDeeplyNestedEncapsulatedDocument_WhenAnonymize_PayloadIsRejectedBeforeMutation()
        {
            var nestedDataset = new DicomDataset
            {
                { DicomTag.PatientName, "Nested^Identifier" },
                { DicomTag.EncapsulatedDocument, Encoding.UTF8.GetBytes(PlantedIdentifier) },
            };
            byte[] originalPayload = nestedDataset.GetValues<byte>(DicomTag.EncapsulatedDocument);

            for (int index = 0; index < NestedSequenceDepth; index++)
            {
                nestedDataset = new DicomDataset(new DicomSequence(DicomTag.ContentSequence, nestedDataset));
            }

            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, "1.2.3.4" },
                new DicomSequence(DicomTag.ContentSequence, nestedDataset),
            };
            DicomDataset payloadDataset = GetNestedDataset(dataset, NestedSequenceDepth + 1);
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-test-engine.json");

            AnonymizerOperationException exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
            Assert.Equal(
                "Unsupported embedded payload rejected. ErrorCode=1201; PayloadTypes=EncapsulatedDocument; DetectedTags=(0042,0011).",
                exception.Message);
            Assert.DoesNotContain(PlantedIdentifier, exception.Message);
            Assert.DoesNotContain("Nested^Identifier", exception.Message);
            Assert.Equal(originalPayload, payloadDataset.GetValues<byte>(DicomTag.EncapsulatedDocument));
            Assert.Equal("Nested^Identifier", payloadDataset.GetSingleValue<string>(DicomTag.PatientName));
        }

        [Fact]
        public void GivenDicomDataSet_SetValidateOutput_WhenAnonymizeWithInvalidOutput_ExceptionWillBeThrown()
        {
            var engine = new AnonymizerEngine("./TestConfigurations/configuration-invalid-string-output.json", new AnonymizerEngineOptions(validateOutput: true));
            Assert.Throws<DicomValidationException>(() => engine.AnonymizeDataset(Dataset));
        }

        private static DicomDataset GetNestedDataset(DicomDataset dataset, int depth)
        {
            DicomDataset current = dataset;
            for (int index = 0; index < depth; index++)
            {
                current = current.GetSequence(DicomTag.ContentSequence).Items[0];
            }

            return current;
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
