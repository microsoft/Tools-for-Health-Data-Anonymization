// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    [Collection("Anonymizer logging state")]
    public class ValidationDiagnosticsTests
    {
        private const string Canary = "SYNTHETIC-VALIDATION-PHI-CANARY";
        private static readonly string InvalidValue = Canary + new string('Z', 65);

        public static IEnumerable<object[]> GetValidationCases()
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                foreach (var validateInput in new[] { false, true })
                {
                    foreach (var nested in new[] { false, true })
                    {
                        yield return new object[] { mode, validateInput, nested };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetValidationCases))]
        public void GivenInvalidMetadata_WhenOptionalValidationFails_OnlyValueFreeDiagnosticsEscape(string mode, bool validateInput, bool nested)
        {
            using var logs = new CapturingLogger();
            using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(logs));
            var originalFactory = AnonymizerLogging.LoggerFactory;
            AnonymizerLogging.LoggerFactory = loggerFactory;
            try
            {
                var file = CreateFile(InvalidValue, nested);
                var engine = CreateEngine(new AnonymizerEngineOptions(validateInput, !validateInput));

                var error = Assert.Throws<AnonymizerOperationException>(() => Apply(engine, file, mode));

                Assert.Equal(
                    validateInput ? DicomAnonymizationErrorCode.InputDatasetValidationFailed : DicomAnonymizationErrorCode.OutputDatasetValidationFailed,
                    error.DicomAnonymizerErrorCode);
                Assert.Equal("DICOM dataset validation failed.", error.Message);
                Assert.Null(error.InnerException);
                Assert.DoesNotContain(Canary, error.Message);
                Assert.DoesNotContain(Canary, error.ToString());
                Assert.All(logs.Messages, message => Assert.DoesNotContain(Canary, message));
                Assert.Equal(validateInput || mode == "clone", file.Dataset.Contains(DicomTag.PatientName));
                Assert.Equal("2.25.123", file.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            }
            finally
            {
                AnonymizerLogging.LoggerFactory = originalFactory;
            }
        }

        [Theory]
        [InlineData("dataset")]
        [InlineData("inplace")]
        [InlineData("clone")]
        public void GivenDefaultOptions_WhenMetadataIsInvalid_ValidationRemainsDisabled(string mode)
        {
            var file = CreateFile(InvalidValue, false);

            var output = Apply(CreateEngine(), file, mode);

            Assert.False(output.Dataset.Contains(DicomTag.PatientName));
            Assert.Equal(InvalidValue, output.Dataset.GetString(DicomTag.InstitutionName));
            Assert.Equal(mode == "clone", file.Dataset.Contains(DicomTag.PatientName));
        }

        [Theory]
        [InlineData("dataset")]
        [InlineData("inplace")]
        [InlineData("clone")]
        public void GivenEmptyValue_WhenBothValidationsAreEnabled_ProcessingSucceeds(string mode)
        {
            var output = Apply(CreateEngine(new AnonymizerEngineOptions(true, true)), CreateFile(string.Empty, false), mode);

            Assert.False(output.Dataset.Contains(DicomTag.PatientName));
            Assert.True(string.IsNullOrEmpty(output.Dataset.GetString(DicomTag.InstitutionName)));
        }

        [Fact]
        public void GivenEmptyDataset_WhenBothValidationsAreEnabled_ProcessingSucceeds()
        {
            var dataset = new DicomDataset();

            CreateEngine(new AnonymizerEngineOptions(true, true)).AnonymizeDataset(dataset);

            Assert.Empty(dataset);
        }

        [Fact]
        public void GivenCustomProcessorValidationFailure_WhenOptionalValidationIsDisabled_ItIsNotReclassified()
        {
            var file = CreateFile(InvalidValue, false);
            var engine = new AnonymizerEngine(
                AnonymizerConfigurationManager.CreateFromJson("{'rules':[{'tag':'PatientName','method':'remove'}]}"),
                processorFactory: new ValidatingProcessorFactory());

            Assert.Throws<DicomValidationException>(() => engine.AnonymizeDataset(file.Dataset));
        }

        [Fact]
        public void GivenValidationErrors_WhenReadingCodes_TheyDoNotReuseExistingCodes()
        {
            Assert.Equal(1106, (int)DicomAnonymizationErrorCode.InputDatasetValidationFailed);
            Assert.Equal(1107, (int)DicomAnonymizationErrorCode.OutputDatasetValidationFailed);
        }

        [Theory]
        [InlineData("dataset", false)]
        [InlineData("inplace", false)]
        [InlineData("clone", false)]
        [InlineData("dataset", true)]
        [InlineData("inplace", true)]
        [InlineData("clone", true)]
        public void GivenInvalidSelectedAge_WhenRedacting_DiagnosticsDoNotExposeTheValue(string mode, bool validateInput)
        {
            var file = CreateFile(string.Empty, false);
            file.Dataset.Add(DicomTag.SelectorASValue, Canary);
            var engine = CreateAgeRedactionEngine(validateInput);

            Exception error;
            if (validateInput)
            {
                var validationError = Assert.Throws<AnonymizerOperationException>(() => Apply(engine, file, mode));
                Assert.Equal(DicomAnonymizationErrorCode.InputDatasetValidationFailed, validationError.DicomAnonymizerErrorCode);
                error = validationError;
            }
            else
            {
                error = Assert.Throws<DicomDataException>(() => Apply(engine, file, mode));
                Assert.Equal("Invalid age string. The valid strings are nnnD, nnnW, nnnM, nnnY.", error.Message);
            }

            Assert.DoesNotContain(Canary, error.Message);
            Assert.DoesNotContain(Canary, error.ToString());
            Assert.Null(error.InnerException);
            Assert.Equal(Canary, file.Dataset.GetString(DicomTag.SelectorASValue));
        }

        [Theory]
        [InlineData("001D")]
        [InlineData("002W")]
        [InlineData("003M")]
        [InlineData("045Y")]
        public void GivenValidSelectedAge_WhenPartialRedactionIsDisabled_ValueIsCleared(string age)
        {
            foreach (var mode in new[] { "dataset", "inplace", "clone" })
            {
                var file = CreateFile(string.Empty, false);
                file.Dataset.Add(DicomTag.SelectorASValue, age);

                var output = Apply(CreateAgeRedactionEngine(true), file, mode);

                Assert.True(output.Dataset.Contains(DicomTag.SelectorASValue));
                Assert.True(string.IsNullOrEmpty(output.Dataset.GetString(DicomTag.SelectorASValue)));
                output.Dataset.Validate();
            }
        }

        [Fact]
        public void GivenInvalidAge_WhenCallingParserDirectly_ErrorRetainsItsTypeWithoutSourceValue()
        {
            var error = Assert.Throws<DicomDataException>(() => DicomUtility.ParseAge(Canary));

            Assert.DoesNotContain(Canary, error.ToString());
            Assert.Null(error.InnerException);
        }

        private static AnonymizerEngine CreateAgeRedactionEngine(bool validateInput) =>
            new AnonymizerEngine(
                AnonymizerConfigurationManager.CreateFromJson(
                    "{'rules':[{'tag':'SelectorASValue','method':'redact','params':{'enablePartialAgesForRedact':false}}]}"),
                new AnonymizerEngineOptions(validateInput, true));

        private static AnonymizerEngine CreateEngine(AnonymizerEngineOptions? options = null)
        {
            var configuration = AnonymizerConfigurationManager.CreateFromJson("{'rules':[{'tag':'PatientName','method':'remove'}]}");
            return options == null ? new AnonymizerEngine(configuration) : new AnonymizerEngine(configuration, options);
        }

        private static DicomFile CreateFile(string value, bool nested)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.SOPInstanceUID, "2.25.123" },
                { DicomTag.PatientName, "SYNTHETIC^NAME" },
            };
            DicomUtility.DisableAutoValidation(dataset);
            var target = nested ? new DicomDataset() : dataset;
            DicomUtility.DisableAutoValidation(target);
            target.Add(DicomTag.InstitutionName, value);
            if (nested)
            {
                dataset.Add(new DicomSequence(DicomTag.RequestAttributesSequence, target));
            }

            return new DicomFile(dataset);
        }

        private static DicomFile Apply(AnonymizerEngine engine, DicomFile file, string mode)
        {
            if (mode == "clone")
            {
                return engine.AnonymizeFile(file);
            }

            if (mode == "inplace")
            {
                engine.AnonymizeFileInPlace(file);
            }
            else
            {
                engine.AnonymizeDataset(file.Dataset);
            }

            return file;
        }

        private sealed class ValidatingProcessorFactory : IAnonymizerProcessorFactory
        {
            public IAnonymizerProcessor CreateProcessor(string method, JObject? settingObject = null) => new ValidatingProcessor();
        }

        private sealed class ValidatingProcessor : IAnonymizerProcessor
        {
            public bool IsSupported(DicomItem item) => true;

            public void Process(DicomDataset dataset, DicomItem item, ProcessContext context) => dataset.Validate();
        }

        private sealed class CapturingLogger : ILoggerProvider, ILogger
        {
            public ConcurrentQueue<string> Messages { get; } = new ConcurrentQueue<string>();

            public ILogger CreateLogger(string categoryName) => this;

            public IDisposable BeginScope<TState>(TState state) => this;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var values = state is IEnumerable<KeyValuePair<string, object>> structured
                    ? string.Join("|", structured.Select(pair => pair.Value?.ToString()))
                    : state?.ToString();
                Messages.Enqueue(formatter(state, exception) + values + exception?.ToString());
            }

            public void Dispose()
            {
            }
        }
    }
}
