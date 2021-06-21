// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.DeID.SharedLib.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class EncryptionProcessorUnitTests
    {
        public EncryptionProcessorUnitTests()
        {
            var encryptObj = "{\"encryptKey\": \"123456781234567812345678\"}";
            Processor = new EncryptProcessor(JObject.Parse(encryptObj));
        }

        public EncryptProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetUnsupportedVRItemForEncryption()
        {
            // Invalid output type
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345" }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345" }; // SL
            yield return new object[] { DicomTag.Real​World​Value​First​Value​Mapped, "12345" }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345" }; // UL
            yield return new object[] { DicomTag.Referenced​Waveform​Channels, "12345\\1234" }; // US
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }; // FD
        }

        public static IEnumerable<object[]> GetSupportedItemForEncryption()
        {
            yield return new object[] { DicomTag.Consulting​Physician​Name, "Test\\Test" }; // PN
            yield return new object[] { DicomTag.Long​Code​Value, "TEST" }; // UC
            yield return new object[] { DicomTag.Event​Timer​Names, "TestTimer" }; // LO
            yield return new object[] { DicomTag.Strain​Additional​Information, "TestInformation" }; // UT
            yield return new object[] { DicomTag.Derivation​Description, "TestDescription" }; // ST
            yield return new object[] { DicomTag.Pixel​Data​Provider​URL, "http://test" }; // LT
        }

        public static IEnumerable<object[]> GetSupportedItemForEncryptionButOutputExceedLengthLimitation()
        {
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST" }; // AE
            yield return new object[] { DicomTag.PatientAge, "100Y" }; // AS
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0" }; // CS
            yield return new object[] { DicomTag.Event​Elapsed​Times, "1234.5" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "1234" }; // IS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345" }; // UI
            yield return new object[] { DicomTag.Consulting​Physician​Name, "jJ7zRxhIpEWWIH9qAIHDyg90+s0wl15xgVP+yt4Agb8=jJ7zRxhIpEWWIH9qAIHDyg90+s0wl15xgVP+yt4Agb8=" };
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForEncryption))]
        public void GivenUnsupportedVRForEncryption_WhenCheckVRIsSupported_ResultWillBeFalse(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.False(Processor.IsSupported(dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetSupportedItemForEncryption))]
        [MemberData(nameof(GetSupportedItemForEncryptionButOutputExceedLengthLimitation))]
        public void GivenSupportedVRForEncryption_WhenCheckVRIsSupported_ResultWillBeTrue(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.True(Processor.IsSupported(dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForEncryption))]
        public void GivenADataSetWithUnsupportedVRForEncryption_WhenEncrypt_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetSupportedItemForEncryptionButOutputExceedLengthLimitation))]
        public void GivenADataSetWithValidVRForEncryption_IfOutputExceedLengthLimitation_WhenEncrypt_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetSupportedItemForEncryptionButOutputExceedLengthLimitation))]
        public void GivenADataSetWithValidVRForEncryption_IfOutputExceedLengthLimitation_WhenEncryptWithoutAutoValidation_ResultWillBeReturned(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };
            dataset.AutoValidate = false;
            var newProcessor = new EncryptProcessor(JObject.Parse("{\"encryptKey\": \"0000000000000000\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            var test = dataset.GetDicomItem<DicomElement>(tag).Get<string>();

            var decryptedValue = string.Join(@"\", dataset.GetDicomItem<DicomElement>(tag).Get<string[]>().Select(x => Decryption(x, "0000000000000000")));
            Assert.Equal(value, decryptedValue);
        }

        [Theory]
        [MemberData(nameof(GetSupportedItemForEncryption))]
        public void GivenADataSetWithValidVRForEncryption_WhenEncryptWithAutoValidation_ResultWillBeReturned(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            var newProcessor = new EncryptProcessor(JObject.Parse("{\"encryptKey\": \"0000000000000000\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            var test = dataset.GetDicomItem<DicomElement>(tag).Get<string>();

            var decryptedValue = string.Join(@"\", dataset.GetDicomItem<DicomElement>(tag).Get<string[]>().Select(x => Decryption(x, "0000000000000000")));
            Assert.Equal(value, decryptedValue);
        }

        [Fact]
        public void GivenADataSetWithDicomElementOB_WhenCheckIsSupported_ResultWillBeTrue()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByte(tag, Encoding.UTF8.GetBytes("test"));

            Assert.True(Processor.IsSupported(item));
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenCheckIsSupported_ResultWillBeTrue()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            Assert.True(Processor.IsSupported(item));
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenCheckIsSupported_ResultWillBeFalse()
        {
            var sps1 = new DicomDataset { { DicomTag.ScheduledStationName, "1" } };
            var sps2 = new DicomDataset { { DicomTag.ScheduledStationName, "2" } };
            var item = new DicomSequence(DicomTag.ScheduledProcedureStepSequence, sps1, sps2);

            Assert.False(Processor.IsSupported(item));
        }

        [Fact]
        public void GivenADataSetWithDicomElementOB_WhenEncrypt_ValueWillBeEncrypted()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByte(tag, Encoding.UTF8.GetBytes("test"));
            var dataset = new DicomDataset(item);

            Processor.Process(dataset, item);

            var key = new byte[] { 49, 50, 51, 52, 53, 54, 55, 56, 49, 50, 51, 52, 53, 54, 55, 56, 49, 50, 51, 52, 53, 54, 55, 56 };
            var base64Key = Convert.ToBase64String(key);
            var decodedKey = Convert.FromBase64String(base64Key);
            var encryptFunction = new EncryptFunction(new EncryptSetting() { EncryptKey = "123456781234567812345678" });
            Assert.Equal(Encoding.UTF8.GetBytes("test"), encryptFunction.DecryptContentWithAES(dataset.GetDicomItem<DicomOtherByte>(tag).Get<byte[]>()));
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenEncrypt_FragmentsWillBeEncrypted()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Encoding.UTF8.GetBytes("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Encoding.UTF8.GetBytes("fragment")));

            var dataset = new DicomDataset(item);

            Processor.Process(dataset, item);

            var enumerator = ((DicomFragmentSequence)dataset.GetDicomItem<DicomItem>(tag)).GetEnumerator();
            var encryptFunction = new EncryptFunction(new EncryptSetting() { EncryptKey = "123456781234567812345678" });
            while (enumerator.MoveNext())
            {
                Assert.Equal(Encoding.UTF8.GetBytes("fragment"), encryptFunction.DecryptContentWithAES(enumerator.Current.Data));
            }
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenEncrypt_ExceptionWillBeThrown()
        {
            var dataset = new DicomDataset { };
            var sps1 = new DicomDataset { { DicomTag.ScheduledStationName, "1" } };
            var sps2 = new DicomDataset { { DicomTag.ScheduledStationName, "2" } };
            var spcs1 = new DicomDataset { { DicomTag.ContextIdentifier, "1" } };
            var spcs2 = new DicomDataset { { DicomTag.ContextIdentifier, "2" } };
            var spcs3 = new DicomDataset { { DicomTag.ContextIdentifier, "3" } };
            sps1.Add(new DicomSequence(DicomTag.ScheduledProtocolCodeSequence, spcs1, spcs2));
            sps2.Add(new DicomSequence(DicomTag.ScheduledProtocolCodeSequence, spcs3));
            dataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, sps1, sps2));

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ScheduledProcedureStepSequence)));
        }

        private string Decryption(string encryptedValue, string key)
        {
            var encryptFunction = new EncryptFunction(new EncryptSetting() { EncryptKey = key });
            return Encoding.UTF8.GetString(encryptFunction.DecryptContentWithAES(Convert.FromBase64String(encryptedValue)));
        }
    }
}