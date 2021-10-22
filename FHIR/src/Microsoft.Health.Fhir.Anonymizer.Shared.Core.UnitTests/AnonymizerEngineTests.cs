using System.IO;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        public AnonymizerEngineTests()
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
        }

        [Fact]
        public void GivenIsPrettyOutputSetTrue_WhenAnonymizeJson_PrettyJsonOutputShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = true
            };
            var result = engine.AnonymizeJson(TestPatientSample, settings);
            Assert.Equal(PrettyOutputTarget, result);
        }

        [Fact]
        public void GivenIsPrettyOutputSetFalse_WhenAnonymizeJson_OneLineJsonOutputShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));

            var result = engine.AnonymizeJson(TestPatientSample);
            Assert.Equal(OneLineOutputTarget, result);
        }

        [Fact]
        public void GivenAnonymizerEngine_AddingCustomProcessor_WhenAnonymize_CorrectResultWillBeReturned()
        {
            var factory = new CustomProcessorFactory();
            factory.AddProcessors(typeof(MaskProcessor));
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-custom-Processor.json"), factory);

            var result = engine.AnonymizeJson(TestPatientSample);
            Assert.Equal(CustomTarget, result);
        }

        [Fact]
        public void GivenAnonymizerEngine_IfConfigurationHasUnsupportedMethod_WhenAnonymize_ExceptionWillBeThrown()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-unsupported-method.json"));

            Assert.Throws<AnonymizerConfigurationException>(() => engine.AnonymizeJson(TestPatientSample));
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
        ""display"": ""redacted""
      }
    ]
  }
}";

        private const string OneLineOutputTarget = "{\"resourceType\":\"Patient\",\"id\":\"example\",\"meta\":{\"security\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/v3-ObservationValue\",\"code\":\"REDACTED\",\"display\":\"redacted\"}]}}";
        private const string CustomTarget = "{\"resourceType\":\"Patient\",\"id\":\"example\",\"name\":[{\"use\":\"***icial\",\"family\":\"***lmers\",\"given\":[\"***er\",\"***es\"]}]}";
    }
}
