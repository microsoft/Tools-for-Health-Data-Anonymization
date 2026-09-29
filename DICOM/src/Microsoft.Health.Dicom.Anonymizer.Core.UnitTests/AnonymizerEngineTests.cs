// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
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

        public static IEnumerable<object[]> GetSupportedCryptoHashSelectors()
        {
            foreach (var selector in new[] { "AE", "CS", "DS", "IS", "SH", "PN", "UC", "LO", "UT", "ST", "LT", "UR", "OB", "UN", "InstanceCreatorUID" })
            {
                yield return new object[] { selector };
            }
        }

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
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"RetrieveAETitle\",\"method\":\"substitute\",\"params\":{\"replaceWith\":\"AnonymousAnonymous\"}}]}");
            var engine = new AnonymizerEngine(manager, new AnonymizerEngineOptions(validateOutput: true));

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
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"RetrieveAETitle\",\"method\":\"substitute\",\"params\":{\"replaceWith\":\"AnonymousAnonymous\"}}]}");
            var engine = new AnonymizerEngine(manager, new AnonymizerEngineOptions(false, false));

            engine.AnonymizeDataset(Dataset);

            Assert.Equal("AnonymousAnonymous", Dataset.GetString(DicomTag.RetrieveAETitle));
            Assert.Throws<DicomValidationException>(() => Dataset.Validate());
        }

        [Theory]
        [InlineData("{\"rules\":[{\"tag\":\"PatientWeight\",\"method\":\"keep\"},{\"tag\":\"(0010,1030)\",\"method\":\"remove\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"UI\",\"method\":\"refreshUID\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"SQ\",\"method\":\"remove\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"StudyDate\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"(0008,00xx)\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"keep\",\"unexpected\":true}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"(7776,1010)\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}")]
        [InlineData("{\"rules\":[{\"tag\":\"PixelData\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}")]
        public void GivenSelectorsAndAnnotations_WhenConstructingEngine_ConfigurationDoesNotRequireDatasetCompatibility(string json)
        {
            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json));

            Assert.NotNull(engine);
        }

        [Fact]
        public void GivenMaskedCryptoHashRule_WhenMatchingAnUnsupportedValue_FailsBeforeDatasetMutation()
        {
            var dataset = new DicomDataset { { DicomTag.StudyDate, "20240101" } };
            var originalValue = dataset.GetString(DicomTag.StudyDate);
            var engine = new AnonymizerEngine(
                AnonymizerConfigurationManager.CreateFromJson(
                    "{\"rules\":[{\"tag\":\"(0008,00xx)\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}"));

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, exception.DicomAnonymizerErrorCode);
            Assert.Equal(originalValue, dataset.GetString(DicomTag.StudyDate));
        }

        [Theory]
        [MemberData(nameof(GetSupportedCryptoHashSelectors))]
        public void GivenSupportedCryptoHashSelector_WhenConstructingEngine_Succeeds(string selector)
        {
            var json = $"{{\"rules\":[{{\"tag\":\"{selector}\",\"method\":\"cryptoHash\"}}],\"defaultSettings\":{{\"cryptoHash\":{{\"cryptoHashKey\":\"key\"}}}}}}";

            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json));

            Assert.NotNull(engine);
        }

        [Fact]
        public void GivenMultiVrCryptoHashTag_WhenAnonymizing_ActualRepresentationDeterminesSupport()
        {
            Assert.Contains(DicomVR.OB, DicomTag.PixelData.DictionaryEntry.ValueRepresentations);
            Assert.Contains(DicomVR.OW, DicomTag.PixelData.DictionaryEntry.ValueRepresentations);
            var json = "{\"rules\":[{\"tag\":\"PixelData\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}";
            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json));
            var supported = new DicomDataset(new DicomOtherByte(DicomTag.PixelData, new byte[] { 1, 2, 3, 4 }));
            var unsupported = new DicomDataset(new DicomOtherWord(DicomTag.PixelData, new ushort[] { 1, 2 }));

            engine.AnonymizeDataset(supported);
            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(unsupported));

            Assert.Equal(DicomVR.OB, supported.GetDicomItem<DicomItem>(DicomTag.PixelData).ValueRepresentation);
            Assert.Equal(32, supported.GetValues<byte>(DicomTag.PixelData).Length);
            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, exception.DicomAnonymizerErrorCode);
            Assert.Equal(new ushort[] { 1, 2 }, unsupported.GetValues<ushort>(DicomTag.PixelData));
        }

        [Fact]
        public void GivenEarlierRuleAndUnsupportedCryptoHashItem_WhenAnonymizing_DatasetRemainsUnchanged()
        {
            var json = "{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"redact\"},{\"tag\":\"FD\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"redact\":{},\"cryptoHash\":{\"cryptoHashKey\":\"key\"}}}";
            var dataset = new DicomDataset
            {
                { DicomTag.PatientName, "PRIVATE" },
                { DicomTag.LongitudinalTemporalOffsetFromEvent, 1.0 },
            };

            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json));

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, exception.DicomAnonymizerErrorCode);
            Assert.Equal("PRIVATE", dataset.GetString(DicomTag.PatientName));
        }

        [Fact]
        public void GivenAnnotationFields_WhenParsingAndAnonymizing_AnnotationsAreIgnored()
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"PatientID\",\"method\":\"keep\",\"note\":\"annotation\"}],\"note\":\"annotation\",\"revision\":1}");
            var dataset = new DicomDataset { { DicomTag.PatientID, "UNCHANGED" } };

            new AnonymizerEngine(manager).AnonymizeDataset(dataset);

            Assert.Equal("UNCHANGED", dataset.GetString(DicomTag.PatientID));
        }

        [Theory]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"keep\"}],\"rules\":[{\"tag\":\"PatientID\",\"method\":\"keep\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"keep\"}],\"Rules\":[{\"tag\":\"PatientID\",\"method\":\"keep\"}]}")]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"tag\":\"PatientID\",\"method\":\"keep\"}]}")]
        public void GivenDuplicateJsonProperty_WhenParsing_BaselineLastValueBehaviorIsPreserved(string json)
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(json);
            var rule = Assert.Single(manager.Configuration.RuleContent);

            Assert.Equal("PatientID", rule["tag"]?.ToString());
            Assert.NotNull(new AnonymizerEngine(manager));
        }

        [Fact]
        public void GivenCaseVariantRuleFields_WhenParsing_CanonicalFieldRetainsBaselineSelection()
        {
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"PatientID\",\"Tag\":\"PatientName\",\"method\":\"remove\"}]}");
            var dataset = new DicomDataset { { DicomTag.PatientID, "REMOVE" }, { DicomTag.PatientName, "UNCHANGED" } };

            new AnonymizerEngine(manager).AnonymizeDataset(dataset);

            Assert.False(dataset.Contains(DicomTag.PatientID));
            Assert.Equal("UNCHANGED", dataset.GetString(DicomTag.PatientName));
        }

        [Theory]
        [InlineData("{\"rules\":\"PRIVATE-CONFIG-SENTINEL\"}", DicomAnonymizationErrorCode.ParsingJsonConfigurationFailed)]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"PRIVATE-CONFIG-SENTINEL\"}]}", DicomAnonymizationErrorCode.UnsupportedAnonymizationRule)]
        [InlineData("{\"rules\":[{\"tag\":\"PRIVATE-CONFIG-SENTINEL\",\"method\":\"keep\"}]}", DicomAnonymizationErrorCode.InvalidConfigurationValues)]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"redact\",\"setting\":\"PRIVATE-CONFIG-SENTINEL\"}]}", DicomAnonymizationErrorCode.MissingRuleSettings)]
        [InlineData("{\"rules\":[{\"tag\":\"PatientName\"}]}", DicomAnonymizationErrorCode.MissingConfigurationFields)]
        [InlineData("{\"rules\":[{\"method\":\"keep\"}]}", DicomAnonymizationErrorCode.MissingConfigurationFields)]
        [InlineData("{\"rules\":[", DicomAnonymizationErrorCode.ParsingJsonConfigurationFailed)]
        public void GivenInvalidPolicyValue_WhenConstructingEngine_DiagnosticDoesNotExposeValue(string json, DicomAnonymizationErrorCode expectedErrorCode)
        {
            var exception = Assert.Throws<AnonymizerConfigurationException>(
                () => new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json)));

            Assert.Equal(expectedErrorCode, exception.DicomAnonymizerErrorCode);
            Assert.DoesNotContain("PRIVATE-CONFIG-SENTINEL", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("PRIVATE-CONFIG-SENTINEL", exception.ToString(), StringComparison.Ordinal);
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

        [Fact]
        public void GivenSamePrivateTagWithDistinctCreators_WhenAnonymizing_RulesRemainIndependent()
        {
            const string creatorA = "SYNTHETIC-CREATOR-A";
            const string creatorB = "SYNTHETIC-CREATOR-B";
            var tagA = DicomTag.Parse($"(0011,1001:{creatorA})");
            var tagB = DicomTag.Parse($"(0011,1001:{creatorB})");
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                $"{{\"rules\":[{{\"tag\":\"(0011,1001:{creatorA})\",\"method\":\"keep\"}},{{\"tag\":\"(0011,1001:{creatorB})\",\"method\":\"remove\"}}]}}");
            var dataset = new DicomDataset();
            dataset.AddOrUpdate(DicomVR.LO, tagA, "KEEP");
            dataset.AddOrUpdate(DicomVR.LO, tagB, "REMOVE");
            var engine = new AnonymizerEngine(manager);

            engine.AnonymizeDataset(dataset);

            Assert.Equal("KEEP", dataset.GetString(tagA));
            Assert.Null(dataset.GetDicomItem<DicomItem>(tagB));
        }

        [Fact]
        public void GivenEquivalentPrivateBlockAliases_WhenAnonymizing_FirstMatchingRuleIsRetained()
        {
            const string creator = "SYNTHETIC-CREATOR-ALIAS-SENTINEL";
            Assert.Equal(DicomTag.Parse($"(0011,1001:{creator})"), DicomTag.Parse($"(0011,1101:{creator})"));
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                $"{{\"rules\":[{{\"tag\":\"(0011,1001:{creator})\",\"method\":\"keep\"}},{{\"tag\":\"(0011,1101:{creator})\",\"method\":\"remove\"}}]}}");

            var tag = DicomTag.Parse($"(0011,1001:{creator})");
            var dataset = new DicomDataset();
            dataset.AddOrUpdate(DicomVR.LO, tag, "UNCHANGED");

            new AnonymizerEngine(manager).AnonymizeDataset(dataset);

            Assert.Equal("UNCHANGED", dataset.GetString(tag));
        }

        [Fact]
        public void GivenPrivateCreatorsDifferingOnlyByCase_WhenConstructingEngine_AcceptsDistinctSelectors()
        {
            var upperTag = DicomTag.Parse("(0011,1001:SYNTHETIC-CREATOR-CASE)");
            var lowerTag = DicomTag.Parse("(0011,1001:synthetic-creator-case)");
            Assert.NotEqual(upperTag, lowerTag);
            var manager = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"(0011,1001:SYNTHETIC-CREATOR-CASE)\",\"method\":\"keep\"},{\"tag\":\"(0011,1001:synthetic-creator-case)\",\"method\":\"remove\"}]}");

            var engine = new AnonymizerEngine(manager);

            Assert.NotNull(engine);
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

        [Fact]
        public void GivenNestedPayloadAndInvalidFileState_WhenAnonymizing_PayloadRejectionTakesPrecedenceWithoutMutation()
        {
            const string originalUid = "2.25.100";
            const string staleFileMetaUid = "2.25.200";
            const string invalidAge = "INVALID";
            byte[] originalPayload = Encoding.UTF8.GetBytes(PlantedIdentifier);
            var nestedDataset = new DicomDataset
            {
                { DicomTag.PatientName, "Nested^Identifier" },
                { DicomTag.EncapsulatedDocument, originalPayload },
            };
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, originalUid },
                new DicomSequence(DicomTag.ContentSequence, nestedDataset),
            };
            DicomUtility.DisableAutoValidation(dataset);
            dataset.Add(DicomTag.PatientAge, invalidAge);
            var file = new DicomFile(dataset);
            file.FileMetaInfo.MediaStorageSOPInstanceUID = new DicomUID(staleFileMetaUid, "Synthetic", DicomUidType.SOPInstance);
            var configuration = AnonymizerConfigurationManager.CreateFromJson(
                "{\"rules\":[{\"tag\":\"PatientName\",\"method\":\"cryptoHash\"}],\"defaultSettings\":{\"cryptoHash\":{\"cryptoHashKey\":\"defaultKey123\"}}}");
            var engine = new AnonymizerEngine(
                configuration,
                new AnonymizerEngineOptions(validateInput: true),
                null,
                null,
                requireRuntimeKeys: true);

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeFile(file));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedEmbeddedPayload, exception.DicomAnonymizerErrorCode);
            Assert.Equal(
                "Unsupported embedded payload rejected. ErrorCode=1201; PayloadTypes=EncapsulatedDocument; DetectedTags=(0042,0011).",
                exception.Message);
            Assert.DoesNotContain(PlantedIdentifier, exception.Message);
            Assert.Equal(originalUid, file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.Equal(staleFileMetaUid, file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(invalidAge, file.Dataset.GetString(DicomTag.PatientAge));
            Assert.Equal(originalPayload, nestedDataset.GetValues<byte>(DicomTag.EncapsulatedDocument));
            Assert.Equal("Nested^Identifier", nestedDataset.GetSingleValue<string>(DicomTag.PatientName));
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
                { DicomTag.FailedSOPInstanceUIDList, "2.25.100", "2.25.200", "2.25.100" },
            };
            var dataset = new DicomDataset
            {
                { DicomTag.FailedSOPInstanceUIDList, "2.25.100", "2.25.200", "2.25.100" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nestedDataset),
            };
            var engine = CreateEngine(new JObject { ["tag"] = "FailedSOPInstanceUIDList", ["method"] = "refreshUID" });

            engine.AnonymizeDataset(dataset);

            var rootValues = dataset.GetValues<string>(DicomTag.FailedSOPInstanceUIDList);
            var nestedValues = nestedDataset.GetValues<string>(DicomTag.FailedSOPInstanceUIDList);
            Assert.Equal(3, rootValues.Length);
            Assert.Equal(rootValues, nestedValues);
            Assert.Equal(rootValues[0], rootValues[2]);
            Assert.NotEqual(rootValues[0], rootValues[1]);
        }

        [Theory]
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
        public void GivenDuplicateExactUidRules_WhenAnonymizing_FirstMatchAppliesAtRootAndInSequences()
        {
            var nested = new DicomDataset { { DicomTag.ReferencedSOPInstanceUID, "2.25.100" } };
            var dataset = new DicomDataset
            {
                { DicomTag.ReferencedSOPInstanceUID, "2.25.100" },
                new DicomSequence(DicomTag.RequestAttributesSequence, nested),
            };
            var engine = CreateEngine(
                new JObject { ["tag"] = "ReferencedSOPInstanceUID", ["method"] = "keep" },
                new JObject { ["tag"] = "ReferencedSOPInstanceUID", ["method"] = "refreshUID" });

            engine.AnonymizeDataset(dataset);

            Assert.Equal("2.25.100", dataset.GetString(DicomTag.ReferencedSOPInstanceUID));
            Assert.Equal("2.25.100", nested.GetString(DicomTag.ReferencedSOPInstanceUID));
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
            var engine = CreateEngine(new JObject { ["tag"] = "SOPClassUID", ["method"] = "refreshUID" });

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, exception.DicomAnonymizerErrorCode);
            Assert.Equal(DicomUID.CTImageStorage, dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID));
        }

        [Theory]
        [InlineData("UI")]
        [InlineData("(0008,xxxx)")]
        public void GivenBroadSelector_WhenAnonymizingAnInvariantUid_InputIsRejectedBeforeMutation(string selector)
        {
            var json = $"{{\"rules\":[{{\"tag\":\"{selector}\",\"method\":\"refreshUID\"}}],\"defaultSettings\":{{}},\"customSettings\":{{}}}}";
            var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromJson(json));
            var dataset = new DicomDataset { { DicomTag.SOPClassUID, DicomUID.CTImageStorage } };

            var exception = Assert.Throws<AnonymizerOperationException>(() => engine.AnonymizeDataset(dataset));

            Assert.Equal(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, exception.DicomAnonymizerErrorCode);
            Assert.Equal(DicomUID.CTImageStorage, dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID));
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
