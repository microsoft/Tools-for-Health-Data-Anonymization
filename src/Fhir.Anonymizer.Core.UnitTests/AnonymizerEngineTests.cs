using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        private readonly AnonymizerEngine _engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));

        [Fact]
        public void GivenIsPrettyOutputSetTrue_WhenAnonymizeJson_PrettyJsonOutputShouldBeReturned()
        {
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = true
            };
            var result = _engine.AnonymizeJson(TestPatientSample, settings);
            Assert.Equal(PrettyOutputTarget, result);
        }

        [Fact]
        public void GivenIsPrettyOutputSetFalse_WhenAnonymizeJson_OneLineJsonOutputShouldBeReturned()
        {
            var result = _engine.AnonymizeJson(TestPatientSample);
            Assert.Equal(OneLineOutputTarget, result);
        }

        private const string TestPatientSample =
@"{
  ""resourceType"": ""Patient"",
  ""id"": ""example"",
  ""name"": [
    {
      ""use"": ""official"",
      ""family"": ""Chalmers"",
      ""given"": [
        ""Peter"",
        ""James""
      ]
    }
  ]
}";

        private const string PrettyOutputTarget =
@"{
  ""resourceType"": ""Patient"",
  ""id"": ""50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c""
}";

        private const string OneLineOutputTarget = "{\"resourceType\":\"Patient\",\"id\":\"50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c\"}";
    }
}
