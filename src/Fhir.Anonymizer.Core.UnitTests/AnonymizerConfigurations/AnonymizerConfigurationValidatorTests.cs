using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class AnonymizerConfigurationValidatorTests
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();

        public static IEnumerable<object[]> GetInvalidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-miss-rules.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-method.json" };
            yield return new object[] { "./TestConfigurations/configuration-invalid-fhirpath.json" };
        }

        [Theory]
        [MemberData(nameof(GetInvalidConfigs))]
        public void GivenAnInvalidConfig_WhenValidate_ExceptionShouldBeThrown(string configFilePath)
        {
            var content = File.ReadAllText(configFilePath);
            var _config = JsonConvert.DeserializeObject<AnonymizerConfiguration>(content);
            Assert.Throws<AnonymizerConfigurationErrorsException>(() => _validator.Validate(_config));
        }
    }
}
