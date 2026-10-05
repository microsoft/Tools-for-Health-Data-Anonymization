// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class DateTimePrecisionTests
    {
        [Theory]
        [InlineData("20240301235959", "20240229235959")]
        [InlineData("20240101132817.1", "20231231132817.1")]
        [InlineData("20240301235959.12+0545", "20240229235959.12+0545")]
        [InlineData("20240301235959.123-0330", "20240229235959.123-0330")]
        [InlineData("20240301235959.1234+0000", "20240229235959.1234+0000")]
        [InlineData("20240301235959.12345+0000", "20240229235959.12345+0000")]
        [InlineData("20240301000000.000001+1400", "20240229000000.000001+1400")]
        [InlineData("20240301235959.123456-1200", "20240229235959.123456-1200")]
        public void GivenSupportedTimestamp_WhenShifted_OnlyCalendarDateChanges(string input, string expected)
        {
            var dataset = new DicomDataset(new DicomDateTime(DicomTag.AcquisitionDateTime, input));
            var processor = new DateShiftProcessor(JObject.Parse("{'dateShiftKey':'','dateShiftRange':1}"));
            processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.AcquisitionDateTime), new ProcessContext());
            Assert.Equal(expected, dataset.GetString(DicomTag.AcquisitionDateTime));
        }

        [Fact]
        public void GivenMultipleFullTimestamps_WhenShifted_TimeAndValueOrderRemainIntact()
        {
            var dataset = new DicomDataset(new DicomDateTime(DicomTag.ReferencedDateTime, "20240301235959.123456", "20240302010203.10-0500"));
            var processor = new DateShiftProcessor(JObject.Parse("{'dateShiftKey':'','dateShiftRange':1}"));
            processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.ReferencedDateTime), new ProcessContext());
            Assert.Equal(new[] { "20240229235959.123456", "20240301010203.10-0500" }, dataset.GetValues<string>(DicomTag.ReferencedDateTime));
        }

        [Theory]
        [InlineData("2024")]
        [InlineData("202402")]
        [InlineData("202403012359")]
        public void GivenPreviouslyUnsupportedPrecision_WhenParsing_ItRemainsExplicitlyUnsupported(string value)
        {
            var error = Assert.Throws<DicomDataException>(() => DicomUtility.ParseDicomDateTime(value));
            Assert.DoesNotContain(value, error.ToString());
        }

        [Fact]
        public void GivenSharedConfigurationProcessor_WhenConcurrentScopesDiffer_OffsetsRemainIsolated()
        {
            var processor = new DateShiftProcessor(JObject.Parse("{'dateShiftKey':'test','dateShiftScope':'StudyInstance'}"));
            Parallel.For(0, 1000, index =>
            {
                string scope = "2.25." + index.ToString(CultureInfo.InvariantCulture);
                int offset = 0;
                foreach (byte value in System.Text.Encoding.UTF8.GetBytes(scope + "test"))
                {
                    offset = ((offset * 131) + value) % 101;
                }

                string expectedDate = new DateTime(2024, 3, 1).AddDays(offset - 50).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                var dataset = new DicomDataset(new DicomDateTime(DicomTag.AcquisitionDateTime, "20240301235959.123456+0545"));
                processor.Process(dataset, dataset.GetDicomItem<DicomItem>(DicomTag.AcquisitionDateTime), new ProcessContext { StudyInstanceUID = scope });
                Assert.Equal(expectedDate + "235959.123456+0545", dataset.GetString(DicomTag.AcquisitionDateTime));
            });
        }
    }
}
