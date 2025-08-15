// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class RuntimeKeySettingsTests
    {
        [Fact]
        public void GivenCryptoHashProcessor_WithRuntimeKey_ShouldProduceDifferentResult()
        {
            // Arrange
            var dataset1 = new DicomDataset
            {
                { DicomTag.PatientName, "John Doe" },
            };

            var dataset2 = new DicomDataset
            {
                { DicomTag.PatientName, "John Doe" },
            };

            var config = CreateTestConfiguration();
            var engine = new AnonymizerEngine(config);

            var runtimeKey1 = new RuntimeKeySettings { CryptoHashKey = "key123" };
            var runtimeKey2 = new RuntimeKeySettings { CryptoHashKey = "key456" };

            // Act
            engine.AnonymizeDataset(dataset1, runtimeKey1);
            engine.AnonymizeDataset(dataset2, runtimeKey2);

            // Assert
            var result1 = dataset1.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
            var result2 = dataset2.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);

            Assert.NotEqual(result1, result2);
            Assert.NotEmpty(result1);
            Assert.NotEmpty(result2);
        }

        [Fact]
        public void GivenCryptoHashProcessor_WithSameRuntimeKey_ShouldProduceSameResult()
        {
            // Arrange
            var dataset1 = new DicomDataset
            {
                { DicomTag.PatientName, "John Doe" },
            };

            var dataset2 = new DicomDataset
            {
                { DicomTag.PatientName, "John Doe" },
            };

            var config = CreateTestConfiguration();
            var engine = new AnonymizerEngine(config);

            var runtimeKey = new RuntimeKeySettings { CryptoHashKey = "key123" };

            // Act
            engine.AnonymizeDataset(dataset1, runtimeKey);
            engine.AnonymizeDataset(dataset2, runtimeKey);

            // Assert
            var result1 = dataset1.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
            var result2 = dataset2.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);

            Assert.Equal(result1, result2);
            Assert.NotEmpty(result1);
        }

        [Fact]
        public void GivenCryptoHashProcessor_WithoutRuntimeKey_ShouldUseConfigurationKey()
        {
            // Arrange
            var dataset1 = new DicomDataset
            {
                { DicomTag.PatientName, "John Doe" },
            };

            var dataset2 = new DicomDataset
            {
                { DicomTag.PatientName, "John Doe" },
            };

            var config = CreateTestConfiguration();
            var engine = new AnonymizerEngine(config);

            // Act
            engine.AnonymizeDataset(dataset1); // No runtime key
            engine.AnonymizeDataset(dataset2, null); // Explicit null runtime key

            // Assert
            var result1 = dataset1.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
            var result2 = dataset2.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);

            Assert.Equal(result1, result2);
            Assert.NotEmpty(result1);
        }

        [Fact]
        public void GivenDateShiftProcessor_WithRuntimeKey_ShouldProduceDifferentResult()
        {
            // Arrange
            var dataset1 = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "1.2.3.4.5.6" },
                { DicomTag.PatientBirthDate, "20000101" },
            };

            var dataset2 = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, "1.2.3.4.5.6" },
                { DicomTag.PatientBirthDate, "20000101" },
            };

            var config = CreateTestConfigurationWithDateShift();
            var engine = new AnonymizerEngine(config);

            var runtimeKey1 = new RuntimeKeySettings { DateShiftKey = "key123" };
            var runtimeKey2 = new RuntimeKeySettings { DateShiftKey = "key456" };

            // Act
            engine.AnonymizeDataset(dataset1, runtimeKey1);
            engine.AnonymizeDataset(dataset2, runtimeKey2);

            // Assert
            var result1 = dataset1.GetSingleValueOrDefault(DicomTag.PatientBirthDate, string.Empty);
            var result2 = dataset2.GetSingleValueOrDefault(DicomTag.PatientBirthDate, string.Empty);

            Assert.NotEqual(result1, result2);
            Assert.NotEqual("20000101", result1); // Should be shifted
            Assert.NotEqual("20000101", result2); // Should be shifted
        }

        private static AnonymizerConfigurationManager CreateTestConfiguration()
        {
            var config = new
            {
                rules = new[]
                {
                    new { tag = "(0010,0010)", method = "cryptoHash" }, // PatientName
                },
                defaultSettings = new
                {
                    cryptoHash = new { cryptoHashKey = "defaultKey123" },
                },
            };

            var json = System.Text.Json.JsonSerializer.Serialize(config);
            return AnonymizerConfigurationManager.CreateFromJson(json);
        }

        private static AnonymizerConfigurationManager CreateTestConfigurationWithDateShift()
        {
            var config = new
            {
                rules = new[]
                {
                    new { tag = "(0010,0030)", method = "dateShift" }, // PatientBirthDate
                },
                defaultSettings = new
                {
                    dateShift = new { dateShiftKey = "defaultDateKey123", dateShiftRange = 50 },
                },
            };

            var json = System.Text.Json.JsonSerializer.Serialize(config);
            return AnonymizerConfigurationManager.CreateFromJson(json);
        }
    }
}