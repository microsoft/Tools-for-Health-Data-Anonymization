// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Xunit;

namespace UnitTests
{
    public class RefreshUIDProccessUnitTests
    {
        public RefreshUIDProccessUnitTests()
        {
            Processor = new RefreshUIDProcessor();
        }

        public RefreshUIDProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetUnsupportedVRItemForRefreshUID()
        {
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST" }; // AE
            yield return new object[] { DicomTag.PatientAge, "100Y" }; // AS
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0" }; // CS
            yield return new object[] { DicomTag.Event​Elapsed​Times, "1234.5" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "1234" }; // IS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST" }; // SH
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345" }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345" }; // SL
            yield return new object[] { DicomTag.Real​World​Value​First​Value​Mapped, "12345" }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345" }; // UL
            yield return new object[] { DicomTag.Referenced​Waveform​Channels, "12345\\1234" }; // US
        }

        public static IEnumerable<object[]> GetValidVRItemForRefreshUID()
        {
            yield return new object[] { DicomTag.Instance​Creator​UID, "1234567890" }; // UI
            yield return new object[] { DicomTag.SOP​Class​UID, "1234567890" }; // UI
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "1234567890" }; // UI
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForRefreshUID))]
        public void GivenADataSetWithUnsupportedVRForCryptoHash_WhenCryptoHash_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetValidVRItemForRefreshUID))]
        public void GivenADataSetWithValidVRForCryptoHash_WhenCryptoHash_ItemWillBeHashed(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.NotEqual(value, dataset.GetDicomItem<DicomElement>(tag).Get<string>());
        }

        [Fact]
        public void GivenADataSetWithSameUID_WhenRefreshUID_TheSameValueWillBeReturned()
        {
            var tag1 = DicomTag.Instance​Creator​UID;
            var tag2 = DicomTag.SOP​Class​UID;

            var dataset = new DicomDataset()
            {
                { tag1, "1234567890" },
                { tag2, "1234567890" },
            };

            var items = dataset.ToArray();
            foreach (var item in items)
            {
                Processor.Process(dataset, item);
            }

            Assert.Equal(dataset.GetDicomItem<DicomElement>(tag2).Get<string>(), dataset.GetDicomItem<DicomElement>(tag1).Get<string>());
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenRefreshUID_ExceptionWillThrown()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByteFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);
            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, item));
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenRefreshUID_ExceptionWillBeThrown()
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

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ScheduledProcedureStepSequence)));
        }
    }
}