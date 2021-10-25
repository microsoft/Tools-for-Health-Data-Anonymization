// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class CustomProcessorFactoryUnitTests
    {
        [Fact]
        public void GivenAFhirProcessorFactory_AddingCustomProcessor_GivenMethod_CorrectProcessorWillBeReturned()
        {
            var factory = new CustomProcessorFactory();
            factory.RegisterProcessors(typeof(MaskProcessor), typeof(MockAnonymizerProcessor));
            Assert.Equal(typeof(MaskProcessor), factory.CreateProcessor("mask", JObject.Parse("{\"maskedLength\":\"3\"}")).GetType());
            Assert.Equal(typeof(MockAnonymizerProcessor), factory.CreateProcessor("mockanonymizer", null).GetType());
        }

        [Fact]
        public void GivenAFhirProcessorFactory_AddingBuildInProcessor_ExceptionWillBeThrown()
        {
            var factory = new CustomProcessorFactory();
            Assert.Throws<AddCustomProcessorException>(() => factory.RegisterProcessors(typeof(RedactProcessor)));
        }
    }
}
