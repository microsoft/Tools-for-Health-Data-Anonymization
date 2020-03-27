using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation;
using Newtonsoft.Json;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations.Validation
{
    public class AnonymizerConfigurationValidatorTests
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();

        public static IEnumerable<object[]> GetInvalidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-miss-rules.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-path.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-type.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-method.json" };
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
