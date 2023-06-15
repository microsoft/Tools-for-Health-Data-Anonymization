// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class DateshiftProcessorUnitTests
    {
        public DateshiftProcessorUnitTests()
        {
            var json = "{\"dateShiftKey\": \"test\"}";
            Processor = new DateShiftProcessor(JObject.Parse(json));
        }

        public DateShiftProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetUnsupportedVRItemForDateShift()
        {
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST" }; // AE
            yield return new object[] { DicomTag.PatientAge, "100Y" }; // AS
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0" }; // CS
            yield return new object[] { DicomTag.Event​Elapsed​Times, "1234.5" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "1234" }; // IS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345" }; // UI
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
        public void GivenUnsupportedVRForDateShift_WhenCheckVRIsSupported_ResultWillBeFalse(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.False(Processor.IsSupported(dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetDAItemForDateshift))]
        [MemberData(nameof(GetDTItemForDateshift))]
        public void GivenSupportedVRForDateShift_WhenCheckVRIsSupported_ResultWillBeTrue(DicomTag tag, string value, string minExpected, string maxExpected)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.True(Processor.IsSupported(dataset.GetDicomItem<DicomElement>(tag)));
            Assert.NotNull(minExpected);
            Assert.NotNull(maxExpected);
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForDateShift))]
        public void GivenADataSetWithUnsupportedVRForDateShift_WhenDateShift_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "123" },
                { DicomTag.SeriesInstanceUID, "456" },
                { DicomTag.SOPInstanceUID, "789" },
                { tag, value },
            };

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), InitContext(dataset)));
        }

        [Theory]
        [MemberData(nameof(GetDAItemForDateshift))]
        public void GivenADataSetWithDAItemForDateshift_WhenDateShift_CorrectValueWillBeReturned(DicomTag tag, string value, string minExpectedValue, string maxExpectedValue)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "123" },
                { DicomTag.SeriesInstanceUID, "456" },
                { DicomTag.SOPInstanceUID, "789" },
                { tag, value },
            };
            var newProcessor = new DateShiftProcessor(JObject.Parse("{\"DateShiftKey\" : \"123\", \"DateShiftScope\" : \"SopInstance\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), InitContext(dataset));
            Assert.InRange(DicomUtility.ParseDicomDate(dataset.GetDicomItem<DicomElement>(tag).Get<string>()), DicomUtility.ParseDicomDate(minExpectedValue), DicomUtility.ParseDicomDate(maxExpectedValue));
        }

        [Theory]
        [MemberData(nameof(GetDTItemForDateshift))]
        public void GivenADataSetWithDTItemForDateshift_WhenDateShift_CorrectValueWillBeReturned(DicomTag tag, string value, string minExpectedValue, string maxExpectedValue)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "123" },
                { DicomTag.SeriesInstanceUID, "456" },
                { DicomTag.SOPInstanceUID, "789" },
                { tag, value },
            };
            var newProcessor = new DateShiftProcessor(JObject.Parse("{\"DateShiftKey\" : \"123\", \"DateShiftScope\" : \"SeriesInstance\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), InitContext(dataset));
            Assert.InRange(DicomUtility.ParseDicomDateTime(dataset.GetDicomItem<DicomElement>(tag).Get<string>()).DateValue, DicomUtility.ParseDicomDateTime(minExpectedValue).DateValue, DicomUtility.ParseDicomDateTime(maxExpectedValue).DateValue);
        }

        [Fact]
        public void GivenADataSetWithDicomElementOB_WhenCheckIsSupported_ResultWillBeFalse()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByte(tag, Encoding.UTF8.GetBytes("test"));

            Assert.False(Processor.IsSupported(item));
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenCheckIsSupported_ResultWillBeFalse()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            Assert.False(Processor.IsSupported(item));
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
        public void GivenADataSetWithDicomElementOB_WhenDateShift_ExceptionWillBeThrown()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByte(tag, Encoding.UTF8.GetBytes("test"));
            var dataset = new DicomDataset(item);

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), InitContext(dataset)));
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenDateshift_ExceptionWillBeThrown()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, item, InitContext(dataset)));
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

            Assert.Throws<AnonymizerOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ScheduledProcedureStepSequence), InitContext(dataset)));
        }

        private ProcessContext InitContext(DicomDataset dataset)
        {
            var context = new ProcessContext
            {
                StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            };
            return context;
        }
    }
}
