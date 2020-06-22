using System.IO;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        public AnonymizerEngineTests()
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
        }

        [Fact]
        public async void GivenIsPrettyOutputSetTrue_WhenAnonymizeJson_PrettyJsonOutputShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = true
            };
            var result = await engine.AnonymizeJson(TestPatientSample, settings);
            Assert.Equal(PrettyOutputTarget, result);
        }

        [Fact]
        public async void GivenIsPrettyOutputSetFalse_WhenAnonymizeJson_OneLineJsonOutputShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));

            var result = await engine.AnonymizeJson(TestPatientSample);
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
  ""id"": ""example"",
  ""meta"": {
    ""security"": [
      {
        ""system"": ""http://terminology.hl7.org/CodeSystem/v3-ObservationValue"",
        ""code"": ""REDACTED"",
        ""display"": ""part of the resource is removed""
      }
    ]
  }
}";

        private const string OneLineOutputTarget = "{\"resourceType\":\"Patient\",\"id\":\"example\",\"meta\":{\"security\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/v3-ObservationValue\",\"code\":\"REDACTED\",\"display\":\"part of the resource is removed\"}]}}";
    }
}
