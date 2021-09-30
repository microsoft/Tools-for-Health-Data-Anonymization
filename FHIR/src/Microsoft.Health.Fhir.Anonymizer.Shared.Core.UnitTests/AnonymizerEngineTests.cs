using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        public void GivenAnonymizerEngine_AddingCustomProcessor_CustomProcessorWillBeAdded()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));

            engine.AddCustomProcessors("test", new MockAnonymizerProcessor());
            var expectedProcessors = engine.GetType().GetField("_processors", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(engine);
            var test = expectedProcessors as Dictionary<string, IAnonymizerProcessor>;
            Assert.Equal(typeof(MockAnonymizerProcessor), test["TEST"].GetType());
        }

        [Fact]
        public void GivenAnonymizerEngine_AddingCustomProcessorWithBuiltInName_CustomProcessorWillBeAdded()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));

            Assert.Throws<AddCustomProcessorException>(() => engine.AddCustomProcessors("redact", new MockAnonymizerProcessor()));
        }

        [Fact]
        public void GivenAnonymizerEngine_IfConfigurationHasUnsupportedMethod_WhenAnonymize_ExceptionWillBeThrown()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-unsupported-method.json"));

            Assert.Throws<AnonymizerConfigurationException>(() => engine.AnonymizeJson(TestPatientSample));
        }

        [Fact]
        public void GivenAnonymizerEngine_IfConfigurationHasUnsupportedMethod_WhenAddingUnsupportedMethodAsCustomProcessor_CorrectResultWillBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-unsupported-method.json"));
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = true
            };

            engine.AddCustomProcessors("skip", new MockAnonymizerProcessor());
            var result = engine.AnonymizeJson(TestPatientSample, settings);

            Assert.Equal(TestPatientSample, result);
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
    }
}
