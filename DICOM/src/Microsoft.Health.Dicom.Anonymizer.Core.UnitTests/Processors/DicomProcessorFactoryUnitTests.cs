// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class DicomProcessorFactoryUnitTests
    {
        [Fact]
        public void GivenADicomProcessorFactory_GivenMethod_CorrectProcessorWillBeReturned()
        {
            var factory = new DicomProcessorFactory();
            Assert.Equal(typeof(PerturbProcessor), factory.CreateProcessor("perturb", new JObject()).GetType());
            Assert.Equal(typeof(EncryptProcessor), factory.CreateProcessor("encrypt", new JObject()).GetType());
            Assert.Equal(typeof(RedactProcessor), factory.CreateProcessor("redact", new JObject()).GetType());
            Assert.Equal(typeof(RefreshUIDProcessor), factory.CreateProcessor("refreshUID", new JObject()).GetType());
            Assert.Equal(typeof(SubstituteProcessor), factory.CreateProcessor("substitute", new JObject()).GetType());
            Assert.Equal(typeof(RemoveProcessor), factory.CreateProcessor("remove", new JObject()).GetType());
            Assert.Equal(typeof(DateShiftProcessor), factory.CreateProcessor("dateshift", new JObject()).GetType());
            Assert.Equal(typeof(CryptoHashProcessor), factory.CreateProcessor("cryptohash", new JObject()).GetType());
        }

        [Fact]
        public void GivenADicomProcessorFactory_AddingCustomProcessor_GivenMethod_CorrectProcessorWillBeReturned()
        {
            var factory = new DicomProcessorFactory();
            factory.AddCustomProcessor("test", new MockAnonymizerProcessor());
            Assert.Equal(typeof(MockAnonymizerProcessor), factory.CreateProcessor("test", new JObject()).GetType());
        }

        [Fact]
        public void GivenADicomProcessorFactory_AddingCustomProcessoWithBuiltInName_ExceptionWillBeThrown()
        {
            var factory = new DicomProcessorFactory();
            Assert.Throws<AddCustomProcessorException>(() => factory.AddCustomProcessor("redact", new MockAnonymizerProcessor()));
        }
    }
}
