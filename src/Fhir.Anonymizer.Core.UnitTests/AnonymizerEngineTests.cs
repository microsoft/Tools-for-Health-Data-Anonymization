using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        private readonly AnonymizerEngine _engine;

        public AnonymizerEngineTests()
        {
            var idTransformer = new ResourceIdTransformer();
            idTransformer.LoadExistingMapping(new Dictionary<string, string> { { "example", "example-abc" } });
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(Path.Combine("TestConfigurations", "configuration-test-sample.json"));
            _engine = new AnonymizerEngine(configurationManager, idTransformer);
        }

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
  ""id"": ""example-abc""
}";

        private const string OneLineOutputTarget = "{\"resourceType\":\"Patient\",\"id\":\"example-abc\"}";
    }
}
