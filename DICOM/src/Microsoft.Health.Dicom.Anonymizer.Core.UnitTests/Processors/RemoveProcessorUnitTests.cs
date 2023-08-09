// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class RemoveProcessorUnitTests
    {
        public RemoveProcessorUnitTests()
        {
            Processor = new RemoveProcessor();
        }

        public RemoveProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetDicomTagsWithValue()
        {
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST" }; // AE
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0" }; // CS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345" }; // UI
            yield return new object[] { DicomTag.Frame​Acquisition​Date​Time, "20200101" }; // DT
            yield return new object[] { DicomTag.Expiry​Date, "20200101" }; // DA
            yield return new object[] { DicomTag.Secondary​Review​Time, "120101.000" }; // TM
            yield return new object[] { DicomTag.Patient​Weight, "53.000" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "10" }; // IS
            yield return new object[] { DicomTag.Patient​Age, "010Y" }; // AS
            yield return new object[] { DicomTag.Patient​Birth​Name, "Name" }; // PN
            yield return new object[] { DicomTag.Strain​Description, "​Description" }; // UC
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345" }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345" }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345" }; // SL
            yield return new object[] { DicomTag.Pixel​Intensity​Relationship​Sign, "12345" }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345" }; // UL
            yield return new object[] { DicomTag.Warning​Reason, "10" }; // US
        }

        [Theory]
        [MemberData(nameof(GetDicomTagsWithValue))]
        public void GivenADataSetWithItemToRemove_WhenRemoving_ItemWillBeRemoved(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.Null(dataset.GetDicomItem<DicomElement>(tag));
        }

        [Fact]
        public void GivenADataSetWithDicomElementOB_WhenRemoveing_ItemWillBeRemoved()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherByte(tag, Encoding.UTF8.GetBytes("test"));
            var dataset = new DicomDataset(item);

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.Null(dataset.GetDicomItem<DicomElement>(tag));
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenRemoveing_ItemWillBeRemoved()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherWordFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);

            Processor.Process(dataset, item);
            Assert.Null(dataset.GetDicomItem<DicomElement>(tag));
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenRemoveing_ItemWillBeRemoved()
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

            Processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ScheduledProcedureStepSequence));
            Assert.Null(dataset.GetDicomItem<DicomElement>(DicomTag.ScheduledProcedureStepSequence));
        }
    }
}