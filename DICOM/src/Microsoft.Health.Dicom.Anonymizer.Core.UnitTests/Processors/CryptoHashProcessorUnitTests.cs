// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class CryptoHashProcessorUnitTests
    {
        public CryptoHashProcessorUnitTests()
        {
            var json = "{\"cryptoHashKey\": \"123\"}";
            Processor = new CryptoHashProcessor(JObject.Parse(json));
        }

        public CryptoHashProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetUnsupportedVRItemForCryptoHash()
        {
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345" }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345" }; // SL
            yield return new object[] { DicomTag.Real​World​Value​First​Value​Mapped, "12345" }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345" }; // UL
            yield return new object[] { DicomTag.Referenced​Waveform​Channels, "12345\\1234" }; // US
        }

        public static IEnumerable<object[]> GetSupportedVRItemForCryptoHash()
        {
            yield return new object[] { DicomTag.Consulting​Physician​Name, "Test\\Test", @"d61ffce34b0192c52d7a67215be73f1e2d640d01383dd8115170b9bd20779a91\d61ffce34b0192c52d7a67215be73f1e2d640d01383dd8115170b9bd20779a91" }; // PN
            yield return new object[] { DicomTag.Long​Code​Value, "TEST", "2e7acefff0307262cef6f503fa7019257f3f9d47fc987fb2c5a31ae4f4d3c022" }; // UC
            yield return new object[] { DicomTag.Event​Timer​Names, "TestTimer", "967df06624010af6b86a019e26aff938976a82e947e96331d8f1fdf387a88089" }; // LO
            yield return new object[] { DicomTag.Strain​Additional​Information, "TestInformation", "70267ad9b166401a6cd6939564dcb70264bb5a62809948e83eebc1a233f43617" }; // UT
            yield return new object[] { DicomTag.Derivation​Description, "TestDescription", "79a5ed3e37eba9bcd14cc30759916ad5df394047a2fed4ad69f1d8ec5edc5337" }; // ST
            yield return new object[] { DicomTag.Pixel​Data​Provider​URL, "http://test", "4983fd14ec2878e50c454764a0d02654ae76fe1001557847b031435100acc9a1" }; // LT
        }

        public static IEnumerable<object[]> GetSupportedVRItemForCryptoHashWithLengthLimitation()
        {
            // Output is constrained to the maximum length and the character repertoire of the value representation
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST", "2e7acefff0307262" }; // AE, 16 characters
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0", "976FEB2C9F52FF3C" }; // CS, 16 uppercase characters
            yield return new object[] { DicomTag.Event​Elapsed​Times, "1234.5", "9219540212405967" }; // DS, 16 digits
            yield return new object[] { DicomTag.Stage​Number, "1234", "217710395" }; // IS, 9 digits
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST", "2e7acefff0307262" }; // SH, 16 characters
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345", "8127147313404203169507445126330115037552104983052141112794412800" }; // UI, 64 digits
        }

        public static IEnumerable<object[]> GetFixedFormatVRItemForCryptoHash()
        {
            yield return new object[] { DicomTag.PatientAge, "100Y" }; // AS
            yield return new object[] { DicomTag.PatientBirthDate, "20210101" }; // DA
            yield return new object[] { DicomTag.AcquisitionDateTime, "20210101120000" }; // DT
            yield return new object[] { DicomTag.StudyTime, "120000" }; // TM
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForCryptoHash))]
        public void GivenUnsupportedVRForCryptoHash_WhenCheckVRIsSupported_ResultWillBeFalse(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.False(Processor.IsSupported(dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetSupportedVRItemForCryptoHash))]
        [MemberData(nameof(GetSupportedVRItemForCryptoHashWithLengthLimitation))]
        public void GivenSupportedVRForCryptoHash_WhenCheckVRIsSupported_ResultWillBeTrue(DicomTag tag, string value, string expectedValue)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.True(Processor.IsSupported(dataset.GetDicomItem<DicomElement>(tag)));
            Assert.NotNull(expectedValue);
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForCryptoHash))]
        public void GivenADataSetWithUnsupportedVRForCryptoHash_WhenCryptoHash_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetSupportedVRItemForCryptoHashWithLengthLimitation))]
        public void GivenADataSetWithSupportedVRForCryptoHash_WhenCryptoHash_OutputWillConformToValueRepresentation(DicomTag tag, string value, string result)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            var hashedValue = dataset.GetDicomItem<DicomElement>(tag).Get<string>();
            Assert.Equal(result, hashedValue);
            Assert.True(tag.DictionaryEntry.ValueRepresentations[0].MaximumLength == 0 || hashedValue.Length <= tag.DictionaryEntry.ValueRepresentations[0].MaximumLength);
        }

        [Fact]
        public void GivenADataSetWithSHValueForCryptoHash_WhenCryptoHash_OutputWillNotExceedSixteenCharacters()
        {
            var tag = DicomTag.PatientTelephoneNumbers; // SH
            var dataset = new DicomDataset
            {
                { tag, "1234567890123456" },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            var hashedValue = dataset.GetDicomItem<DicomElement>(tag).Get<string>();
            Assert.Equal(16, hashedValue.Length);
        }

        [Fact]
        public void GivenADataSetWithSHValueForCryptoHash_WhenCryptoHashTwice_OutputWillBeDeterministic()
        {
            var tag = DicomTag.PatientTelephoneNumbers; // SH
            var firstDataset = new DicomDataset { { tag, "TEST" } };
            var secondDataset = new DicomDataset { { tag, "TEST" } };

            Processor.Process(firstDataset, firstDataset.GetDicomItem<DicomElement>(tag));
            Processor.Process(secondDataset, secondDataset.GetDicomItem<DicomElement>(tag));

            Assert.Equal(firstDataset.GetDicomItem<DicomElement>(tag).Get<string>(), secondDataset.GetDicomItem<DicomElement>(tag).Get<string>());
        }

        [Fact]
        public void GivenADataSetWithSHValueForCryptoHash_WhenCryptoHashWithMatchInputStringLength_OutputWillMatchInputLength()
        {
            var processor = new CryptoHashProcessor(JObject.Parse("{\"cryptoHashKey\": \"123\", \"matchInputStringLength\": true}"));
            var tag = DicomTag.PatientTelephoneNumbers; // SH
            var dataset = new DicomDataset { { tag, "1234567890123456" } };

            processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.Equal(16, dataset.GetDicomItem<DicomElement>(tag).Get<string>().Length);
        }

        [Theory]
        [MemberData(nameof(GetFixedFormatVRItemForCryptoHash))]
        public void GivenADataSetWithFixedFormatVRForCryptoHash_WhenCryptoHash_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            var exception = Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
            Assert.DoesNotContain(value, exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(GetSupportedVRItemForCryptoHash))]
        public void GivenADataSetWithValidVRForCryptoHash_WhenCryptoHash_ItemWillBeHashed(DicomTag tag, string value, string result)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.Equal(result, dataset.GetDicomItem<DicomElement>(tag).Get<string>());
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
        public void GivenADataSetWithDicomElementOB_WhenCryptoHash_ValueWillBeHashed()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByte(tag, Encoding.UTF8.GetBytes("test"));
            var dataset = new DicomDataset(item);

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            var resultBytes = dataset.GetDicomItem<DicomOtherByte>(tag).Get<byte[]>();
            Assert.Equal("a7f5c8c626f994482813230854f66700e626208f52d913b9bd6b4e039aab0f41", string.Concat(resultBytes.Select(b => b.ToString("x2"))));
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenCryptoHash_FragmentsWillBeHashed()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);

            Processor.Process(dataset, dataset.GetDicomItem<DicomOtherByteFragment>(tag));

            var enumerator = ((DicomFragmentSequence)dataset.GetDicomItem<DicomItem>(tag)).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var resultString = string.Concat(enumerator.Current.Data.Select(b => b.ToString("x2")));
                Assert.Equal("1ad1011bc425028a63a257140287a08d38d0f203e4bdf063b077acf6eca651a9", resultString);
            }
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenCryptoHash_ExceptionWillBeThrown()
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
    }
}
