﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class CustomProcessorFactoryUnitTests
    {
        [Fact]
        public void GivenADicomProcessorFactory_GivenMethod_CorrectProcessorWillBeReturned()
        {
            var factory = new CustomProcessorFactory();
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
            var factory = new CustomProcessorFactory();
            factory.RegisterProcessors(typeof(MaskProcessor), typeof(MockAnonymizerProcessor));
            Assert.Equal(typeof(MaskProcessor), factory.CreateProcessor("mask", JObject.Parse("{\"maskedLength\":\"3\"}")).GetType());
            Assert.Equal(typeof(MockAnonymizerProcessor), factory.CreateProcessor("mockanonymizer", new JObject()).GetType());
        }

        [Fact]
        public void GivenADicomProcessorFactory_AddingBuildInProcessor_ExceptionWillBeThrown()
        {
            var factory = new CustomProcessorFactory();
            Assert.Throws<AddCustomProcessorException>(() => factory.RegisterProcessors(typeof(RedactProcessor)));
        }
    }
}
