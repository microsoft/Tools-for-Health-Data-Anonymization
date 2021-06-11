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
using Newtonsoft.Json.Linq;
using Xunit;

namespace UnitTests
{
    public class SubstituteProcessUnitTests
    {
        public SubstituteProcessUnitTests()
        {
            Processor = new SubstituteProcessor(JObject.Parse("{\"ReplaceWith\" : \"Anonymous\"}"));
        }

        public SubstituteProcessor Processor { get; set; }

        public static IEnumerable<object[]> GetValidSettingForSubstitute()
        {
            // string element
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST", JObject.Parse("{\"ReplaceWith\" : \"Anonymous\"}"), "Anonymous" }; // AE
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0", JObject.Parse("{\"ReplaceWith\" : \"1\"}"), "1" }; // CS
            yield return new object[] { DicomTag.Patient​Telephone​Numbers, "TEST", JObject.Parse("{}"), "Anonymous" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345", JObject.Parse("{\"ReplaceWith\" : \"10000\"}"), "10000" }; // UI
            yield return new object[] { DicomTag.Frame​Acquisition​Date​Time, "20200101", JObject.Parse("{\"ReplaceWith\" : \"20000101\"}"), "20000101" }; // DT
            yield return new object[] { DicomTag.Expiry​Date, "20200101", JObject.Parse("{\"ReplaceWith\" : \"20000101\"}"), "20000101" }; // DA
            yield return new object[] { DicomTag.Secondary​Review​Time, "120101.000", JObject.Parse("{\"ReplaceWith\" : \"000000.000\"}"), "000000.000" }; // TM
            yield return new object[] { DicomTag.Patient​Weight, "53.000", JObject.Parse("{\"ReplaceWith\" : \"50.1\"}"), "50.1" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "10", JObject.Parse("{\"ReplaceWith\" : \"20\"}"), "20" }; // IS
            yield return new object[] { DicomTag.Patient​Age, "010Y", JObject.Parse("{\"ReplaceWith\" : \"020M\"}"), "020M" }; // AS
            yield return new object[] { DicomTag.Patient​Birth​Name, "Name", JObject.Parse("{\"ReplaceWith\" : \"Name=Name=Name\"}"), "Name=Name=Name" }; // PN
            yield return new object[] { DicomTag.Strain​Description, "​Description", JObject.Parse("{}"), "Anonymous" }; // UC

            // AT element
            // yield return new object[] { DicomTag.Dimension​Index​Pointer, "​00100010", new DicomSubstituteSetting { ReplaceWith = "11001100" }, "(1100,1100)" }; // AT

            // value element
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345", JObject.Parse("{\"ReplaceWith\" : \"23456\"}"), "23456" }; // FD
            yield return new object[] { DicomTag.Examined​Body​Thickness, "12345", JObject.Parse("{\"ReplaceWith\" : \"23456\"}"), "23456" }; // FL
            yield return new object[] { DicomTag.Doppler​Sample​Volume​X​Position, "12345", JObject.Parse("{\"ReplaceWith\" : \"23456\"}"), "23456" }; // SL
            yield return new object[] { DicomTag.Pixel​Intensity​Relationship​Sign, "12345", JObject.Parse("{\"ReplaceWith\" : \"-23456\"}"), "-23456" }; // SS
            yield return new object[] { DicomTag.Referenced​Content​Item​Identifier, "12345", JObject.Parse("{\"ReplaceWith\" : \"23456\"}"), "23456" }; // UL
            yield return new object[] { DicomTag.Warning​Reason, "10", JObject.Parse("{\"ReplaceWith\" : \"20\"}"), "20" }; // US
        }

        public static IEnumerable<object[]> GetInvalidStringFormatForSubstitute()
        {
            yield return new object[] { DicomTag.RetrieveAETitle, "TEST", JObject.Parse("{\"ReplaceWith\" : \"AnonymousAnonymousAnonymous\"}"), "AnonymousAnonymousAnonymous" }; // AE 16bytes maximum
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0", JObject.Parse("{\"ReplaceWith\" : \"Anonymous\"}"), "Anonymous" }; // CS 16bytes maximum Uppercase characters, "0"-"9", the SPACE character, and underscore "_"
            yield return new object[] { DicomTag.Ethnic​Group, "TEST", JObject.Parse("{\"ReplaceWith\" : \"AnonymousAnonymousAnonymous\"}"), "AnonymousAnonymousAnonymous" }; // SH
            yield return new object[] { DicomTag.SOP​Classes​In​Study, "12345", JObject.Parse("{}"), "Anonymous" }; // UI "0"-"9", "."
            yield return new object[] { DicomTag.Frame​Acquisition​Date​Time, "20200101", JObject.Parse("{}"), "Anonymous" }; // DT YYYYMMDDHHMMSS.FFFFFF&ZZXX
            yield return new object[] { DicomTag.Expiry​Date, "20200101", JObject.Parse("{\"ReplaceWith\" : \"2000-01-01\"}"), "2000-01-01" }; // DA YYYYMMDD
            yield return new object[] { DicomTag.Secondary​Review​Time, "120101.000", JObject.Parse("{\"ReplaceWith\" : \"invalid time\"}"), "invalid time" }; // TM HHMMSS.FFFFFF
            yield return new object[] { DicomTag.Patient​Weight, "53.000", JObject.Parse("{}"), "Anonymous" }; // DS
            yield return new object[] { DicomTag.Stage​Number, "10", JObject.Parse("{}"), "Anonymous" }; // IS
            yield return new object[] { DicomTag.Patient​Age, "010Y", JObject.Parse("{\"ReplaceWith\" : \"200\"}"), "200" }; // AS
            yield return new object[] { DicomTag.Patient​Birth​Name, "Name", JObject.Parse("{\"ReplaceWith\" : \"Name=Name=Name=Name\"}"), "Name=Name=Name=Name" }; // PN
        }

        public static IEnumerable<object[]> GetInvalidStringVMForSubstitute()
        {
            yield return new object[] { DicomTag.Station​AE​Title, "TEST", JObject.Parse("{\"ReplaceWith\" : \"Anonymous\\\\Anonymous\\\\Anonymous\"}"), "Anonymous\\Anonymous\\Anonymous" }; // AE 16bytes maximum
            yield return new object[] { DicomTag.Query​Retrieve​Level, "0", JObject.Parse("{\"ReplaceWith\" : \"0\\\\1\\\\2\"}"), "0\\1\\2" }; // CS 16bytes maximum Uppercase characters, "0"-"9", the SPACE character, and underscore "_"
            yield return new object[] { DicomTag.Ethnic​Group, "TEST", JObject.Parse("{\"ReplaceWith\" : \"Anonymous\\\\Anonymous\"}"), "Anonymous\\Anonymous" }; // SH
            yield return new object[] { DicomTag.Expiry​Date, "20200101", JObject.Parse("{\"ReplaceWith\" : \"2000-01-01\\\\2000-01-01\"}"), "2000-01-01\\2000-01-01" }; // DA YYYYMMDD
            yield return new object[] { DicomTag.Stage​Number, "1234", JObject.Parse("{\"ReplaceWith\" : \"23456\\\\123\"}"), "23456\\123" }; // IS
        }

        public static IEnumerable<object[]> GetInvalidReplaceValueTypeForSubstitute()
        {
            yield return new object[] { DicomTag.Longitudinal​Temporal​Offset​From​Event, "12345", JObject.Parse("{}"), "Anonymous" }; // FD
        }

        [Theory]
        [MemberData(nameof(GetValidSettingForSubstitute))]
        public void GivenADataSetWithValidVRForSubstitute_WhenSubstitute_ValueWillBeReplaced(DicomTag tag, string value, JObject settings, string replaceWith)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            var newProcessor = new SubstituteProcessor(settings);
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.Equal(replaceWith, dataset.GetDicomItem<DicomElement>(tag).Get<string>());
        }

        [Theory]
        [MemberData(nameof(GetInvalidStringFormatForSubstitute))]
        [MemberData(nameof(GetInvalidStringVMForSubstitute))]
        [MemberData(nameof(GetInvalidReplaceValueTypeForSubstitute))]
        public void GivenADataSetWithInvalidReplaceValueForSubstitute_WhenSubstitute_ExceptionWillBeThrown(DicomTag tag, string value, JObject settings, string replaceWith)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };
            var newProcessor = new SubstituteProcessor(settings);
            Assert.Throws<AnonymizerConfigurationException>(() => newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Theory]
        [MemberData(nameof(GetInvalidStringFormatForSubstitute))]
        [MemberData(nameof(GetInvalidStringVMForSubstitute))]
        public void GivenADataSetWithInvalidStringAndVMForSubstitute_WhenSubstituteWithoutAutoValidation_ResultWillBeReturned(DicomTag tag, string value, JObject settings, string replaceWith)
        {
            var dataset = new DicomDataset
            {
                { tag, value },
            };

            dataset.AutoValidate = false;
            var newProcessor = new SubstituteProcessor(settings);
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.Equal(replaceWith, dataset.GetDicomItem<DicomElement>(tag).Get<string>());
        }

        [Fact]
        public void GivenADataSetWithOWForSubstitute_WhenSubstitute_ValueWillBeSubstituted()
        {
            var tag = DicomTag.Red​Palette​Color​Lookup​Table​Data;
            var dataset = new DicomDataset
            {
                { tag, (ushort)10 },
            };

            var newProcessor = new SubstituteProcessor(JObject.Parse("{\"ReplaceWith\" : \"20\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.True(dataset.GetDicomItem<DicomElement>(tag).Get<ushort>() == 20);
        }

        [Fact]
        public void GivenADataSetWithOWForSubstitute_WhenSubstituteWithInvalidValue_ExceptionWillBeThrown()
        {
            var tag = DicomTag.Red​Palette​Color​Lookup​Table​Data;
            var dataset = new DicomDataset
            {
                { tag, (ushort)10 },
            };

            Assert.Throws<AnonymizerConfigurationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag)));
        }

        [Fact]
        public void GivenADataSetWithOLForSubstitute_WhenSubstitute_ValueWillBeSubstituted()
        {
            var tag = DicomTag.Selector​OLValue;
            var dataset = new DicomDataset
            {
                { tag, 10U },
            };

            var newProcessor = new SubstituteProcessor(JObject.Parse("{\"ReplaceWith\" : \"20\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.True(dataset.GetDicomItem<DicomElement>(tag).Get<uint>() == 20);
        }

        [Fact]
        public void GivenADataSetWithODForSubstitute_WhenSubstitute_ValueWillBeSubstituted()
        {
            var tag = DicomTag.Volumetric​Curve​Up​Directions;
            var dataset = new DicomDataset
            {
                { tag, 10D },
            };

            var newProcessor = new SubstituteProcessor(JObject.Parse("{\"ReplaceWith\" : \"20\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.True(dataset.GetDicomItem<DicomElement>(tag).Get<double>() == 20);
        }

        [Fact]
        public void GivenADataSetWithOFForSubstitute_WhenSubstitute_ValueWillBeSubstituted()
        {
            var tag = DicomTag.Float​​Pixel​​Data;
            var dataset = new DicomDataset
            {
                { tag, 10F },
            };

            var newProcessor = new SubstituteProcessor(JObject.Parse("{\"ReplaceWith\" : \"20\"}"));
            newProcessor.Process(dataset, dataset.GetDicomItem<DicomElement>(tag));
            Assert.True(dataset.GetDicomItem<DicomElement>(tag).Get<float>() == 20);
        }

        [Fact]
        public void GivenADataSetWithDicomFragmentSequence_WhenSubstitute_ExceptionWillBeThrown()
        {
            var tag = DicomTag.PixelData;
            var item = new DicomOtherWordFragment(tag);
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));
            item.Fragments.Add(new MemoryByteBuffer(Convert.FromBase64String("fragment")));

            var dataset = new DicomDataset(item);

            Assert.Throws<AnonymizerConfigurationException>(() => Processor.Process(dataset, item));
        }

        [Fact]
        public void GivenADataSetWithSQItem_WhenSubstitute_ExceptionWillBeThrown()
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

            Assert.Throws<AnonymizerConfigurationException>(() => Processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ScheduledProcedureStepSequence)));
        }
    }
}