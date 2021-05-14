// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace UnitTests
{
    public class DateshiftProcessUnitTests
    {
        public DateshiftProcessUnitTests()
        {
            Processor = new DateShiftProcessor(new DicomDateShiftSetting() { DateShiftKey = "test" });
        }

        public DateShiftProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetUnsupportedVRItemForDateShift()
        {
            // Invalid output length limitation
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST" }; // AE
            yield return new object[] { DicomTag.PatientAge, "100Y" }; // AS
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0" }; // CS
            yield return new object[] { DicomTag.Event​Elapsed​Times, "1234.5" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "1234" }; // IS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345" }; // UI

            // Invalid input
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345" }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345" }; // SL
            yield return new object[] { DicomTag.Real​World​Value​First​Value​Mapped, "12345" }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345" }; // UL
            yield return new object[] { DicomTag.Referenced​Waveform​Channels, "12345\\1234" }; // US
        }

        public static IEnumerable<object[]> GetDAItemForDateshift()
        {
            yield return new object[] { DicomTag.Patient​Birth​Date, "20150207", "20141219", "20150329" }; // DA
            yield return new object[] { DicomTag.Instance​Creation​Date, "20200117", "20191128", "20200307" }; // DA
            yield return new object[] { DicomTag.Study​Date, "19981002", "19980813", "19981121" }; // DA
        }

        public static IEnumerable<object[]> GetDTItemForDateshift()
        {
            yield return new object[] { DicomTag.Instance​Coercion​Date​Time, "20150207132817-0500", "20141219000000-0500", "20150329000000-0500" }; // DT
            yield return new object[] { DicomTag.Radio​pharmaceutical​Start​Date​Time, "20150207132817", "20141219000000", "20150329000000" }; // DT
            yield return new object[] { DicomTag.Frame​Acquisition​Date​Time, "19981002084725.1234+0800", "19980813000000+0800", "19981121000000+0800" }; // DT
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForDateShift))]
        public void GivenADataSetWithUnsupportedVRForDateShift_WhenCryptoHash_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "123" },
                { DicomTag.SeriesInstanceUID, "456" },
                { DicomTag.SOPInstanceUID, "789" },
                { tag, value },
            };

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), ExtractBasicInformation(dataset)));
        }

        [Theory]
        [MemberData(nameof(GetDAItemForDateshift))]
        public void GivenADataSetWithDAItemForDateshift_WhenCryptoHash_ItemWillBeHashed(DicomTag tag, string value, string minExpectedValue, string maxExpectedValue)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "123" },
                { DicomTag.SeriesInstanceUID, "456" },
                { DicomTag.SOPInstanceUID, "789" },
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), ExtractBasicInformation(dataset), new DicomDateShiftSetting() { DateShiftKey = "123", DateShiftScope = DateShiftScope.SopInstance });
            Assert.InRange(Utility.ParseDicomDate(dataset.GetDicomItem<DicomElement>(tag).Get<string>()), Utility.ParseDicomDate(minExpectedValue), Utility.ParseDicomDate(maxExpectedValue));
        }

        [Theory]
        [MemberData(nameof(GetDTItemForDateshift))]
        public void GivenADataSetWithDTItemForDateshift_WhenCryptoHash_ItemWillBeHashed(DicomTag tag, string value, string minExpectedValue, string maxExpectedValue)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "123" },
                { DicomTag.SeriesInstanceUID, "456" },
                { DicomTag.SOPInstanceUID, "789" },
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), ExtractBasicInformation(dataset), new DicomDateShiftSetting() { DateShiftKey = "123", DateShiftScope = DateShiftScope.SeriesInstance });
            Assert.InRange(Utility.ParseDicomDateTime(dataset.GetDicomItem<DicomElement>(tag).Get<string>()).DateValue, Utility.ParseDicomDateTime(minExpectedValue).DateValue, Utility.ParseDicomDateTime(maxExpectedValue).DateValue);
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenDateshift_ExceptionWillBeThrown()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, item, ExtractBasicInformation(dataset)));
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenDateshift_ExceptionWillBeThrown()
        {
            var dataset = new DicomDataset { { DicomTag.StudyInstanceUID, "123" }, { DicomTag.SeriesInstanceUID, "456" }, { DicomTag.SOPInstanceUID, "789" } };
            var sps1 = new DicomDataset { { DicomTag.ScheduledStationName, "1" } };
            var sps2 = new DicomDataset { { DicomTag.ScheduledStationName, "2" } };
            var spcs1 = new DicomDataset { { DicomTag.ContextIdentifier, "1" } };
            var spcs2 = new DicomDataset { { DicomTag.ContextIdentifier, "2" } };
            var spcs3 = new DicomDataset { { DicomTag.ContextIdentifier, "3" } };
            sps1.Add(new DicomSequence(DicomTag.ScheduledProtocolCodeSequence, spcs1, spcs2));
            sps2.Add(new DicomSequence(DicomTag.ScheduledProtocolCodeSequence, spcs3));
            dataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, sps1, sps2));

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ScheduledProcedureStepSequence), ExtractBasicInformation(dataset)));
        }

        private DicomBasicInformation ExtractBasicInformation(DicomDataset dataset)
        {
            var basicInfo = new DicomBasicInformation
            {
                StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            };
            return basicInfo;
        }
    }
}