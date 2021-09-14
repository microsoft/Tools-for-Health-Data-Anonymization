using Microsoft.Health.Anonymizer.Common.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using System;
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
            factory.AddCustomerProcessor("test", new MockAnonymizerProcessor());
            Assert.Equal(typeof(MockAnonymizerProcessor), factory.CreateProcessor("test", new JObject()).GetType());
        }
    }
}
