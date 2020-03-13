using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class AnonymizerEngineTests
    {
        private readonly AnonymizerEngine _engine = new AnonymizerEngine(Path.Combine("TestConfigurations", "configuration-test-sample.json"));

        [Fact]
        public void GivenIsPrettyOutputSetTrue_WhenAnonymizeJson_PrettyJsonOutputShouldBeReturned()
        {
            var result = _engine.AnonymizeJson(TestPatientSample, isPrettyOutput: true);
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
  ""id"": ""example"",
  ""name"": [
    {
      ""use"": """",
      ""family"": """",
      ""given"": [
        """",
        """"
      ]
    }
  ]
}";

        private const string OneLineOutputTarget = "{\"resourceType\":\"Patient\",\"id\":\"example\",\"name\":[{\"use\":\"\",\"family\":\"\",\"given\":[\"\",\"\"]}]}";
    }
}
