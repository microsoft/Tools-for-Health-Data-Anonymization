// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Xunit;

namespace UnitTests
{
    public class PerturbProcessUnitTests
    {
        public PerturbProcessUnitTests()
        {
            Processor = new PerturbProcessor(new DicomPerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional });
        }

        public PerturbProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetUnsupportedVRItemForPerturb()
        {
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST" }; // AE
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0" }; // CS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345" }; // UI
            yield return new object[] { DicomTag.Consulting​Physician​Name, "Test\\Test" }; // PN
            yield return new object[] { DicomTag.Long​Code​Value, "TEST" }; // UC
            yield return new object[] { DicomTag.Event​Timer​Names, "TestTimer" }; // LO
            yield return new object[] { DicomTag.Strain​Additional​Information, "TestInformation" }; // UT
            yield return new object[] { DicomTag.Derivation​Description, "TestDescription" }; // ST
            yield return new object[] { DicomTag.Pixel​Data​Provider​URL, "http://test" }; // LT
            yield return new object[] { DicomTag.Frame​Acquisition​Date​Time, "20200101" }; // DT
            yield return new object[] { DicomTag.Expiry​Date, "20200101" }; // DA
            yield return new object[] { DicomTag.Secondary​Review​Time, "120101.000" }; // TM
        }

        public static IEnumerable<object[]> GetValidVRItemForPerturb()
        {
            yield return new object[] { DicomTag.Stage​Number, "1234", new DicomPerturbSetting() { Span = 1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 617, 1851 }; // IS
            yield return new object[] { DicomTag.Event​Elapsed​Times, "1234.5", new DicomPerturbSetting() { Span = 1000, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 734.5, 1734.5 }; // DS
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345", new DicomPerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional }, 6172.5, 18517.5 }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345", new DicomPerturbSetting() { Span = 1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 6172.5, 18517.5 }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345", new DicomPerturbSetting() { Span = 1, RoundTo = 0, RangeType = PerturbRangeType.Proportional }, 6173, 18518 }; // SL
            yield return new object[] { DicomTag.Pixel​Intensity​Relationship​Sign, "12345", new DicomPerturbSetting() { Span = 10000, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 7345, 17345 }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345", new DicomPerturbSetting() { Span = 100000, RoundTo = 2, RangeType = PerturbRangeType.Fixed }, 0, 62345 }; // UL
            yield return new object[] { DicomTag.Warning​Reason, "10", null, 5, 15 }; // FS
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedVRItemForPerturb))]
        public void GivenADataSetWithUnsupportedVRForPerturb_WhenPerturb_ExceptionWillBeThrown(DicomTag tag, string value)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetValidVRItemForPerturb))]
        public void GivenADataSetWithValidVRForPerturb_WhenPerturb_ValueWillBePerturbed(DicomTag tag, string value, DicomPerturbSetting settings, decimal minExpectedValue, decimal maxExpectedValue)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag), null, settings);
            foreach (var item in dataset.GetDicomItem<DicomElement>(tag).Get<string[]>())
            {
                Assert.InRange(decimal.Parse(item), minExpectedValue, maxExpectedValue);
            }
        }

        [Fact]
        public void GivenADataSetWithDicomElementAS_WhenPerturb_AgeValueWillBePerturbed()
        {
            var tag = DicomTag.PatientAge;
            var item = new DicomAgeString(tag, "050Y");
            var dataset = new DicomDataset(item);

            Processor.Process(dataset, item, null, new DicomPerturbSetting() { Span = 1, RoundTo = 2, RangeType = PerturbRangeType.Proportional });
            Assert.InRange(int.Parse(dataset.GetDicomItem<DicomAgeString>(tag).Get<string>().Substring(0, 3)), 25, 75);
        }

        [Fact]
        public void GivenADataSetWithOWForPerturb_WhenPerturb_ValueWillBePerturbed()
        {
            var tag = DicomTag.Red​Palette​Color​Lookup​Table​Data;
            var dataset = new DicomDataset
            {
                { tag, (ushort)10 },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.InRange<ushort>(dataset.GetDicomItem<DicomElement>(tag).Get<ushort>(), 5, 15);
        }

        [Fact]
        public void GivenADataSetWithOLForPerturb_WhenPerturb_ValueWillBePerturbed()
        {
            var tag = DicomTag.Selector​OLValue;
            var dataset = new DicomDataset
            {
                { tag, 10U },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.InRange<uint>(dataset.GetDicomItem<DicomElement>(tag).Get<uint>(), 5, 15);
        }

        [Fact]
        public void GivenADataSetWithODForPerturb_WhenPerturb_ValueWillBePerturbed()
        {
            var tag = DicomTag.Volumetric​Curve​Up​Directions;
            var dataset = new DicomDataset
            {
                { tag, 10D },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.InRange<double>(dataset.GetDicomItem<DicomElement>(tag).Get<double>(), 5, 15);
        }

        [Fact]
        public void GivenADataSetWithOFForPerturb_WhenPerturb_ValueWillBePerturbed()
        {
            var tag = DicomTag.Float​​Pixel​​Data;
            var dataset = new DicomDataset
            {
                { tag, 10F },
            };

            Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.InRange<float>(dataset.GetDicomItem<DicomElement>(tag).Get<float>(), 5, 15);
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenPerturb_ExceptionWillBeThrown()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherWordFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);

            Assert.Throws<AnonymizationOperationException>(() => Processor.Process(dataset, item));
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenPerturb_ExceptionWillBeThrown()
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