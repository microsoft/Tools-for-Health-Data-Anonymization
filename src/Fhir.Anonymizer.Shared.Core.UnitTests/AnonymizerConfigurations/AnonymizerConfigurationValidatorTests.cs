using System.Collections.Generic;
using System.IO;
using Microsoft.Health.Fhir23.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Fhir23.Anonymizer.Core.UnitTests
{
    public class AnonymizerConfigurationValidatorTests
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();

        public static IEnumerable<object[]> GetInvalidConfigs()
        {
            yield return new object[] { "./TestConfigurations/configuration-miss-rules.json" };
            yield return new object[] { "./TestConfigurations/configuration-unsupported-method.json" };
            yield return new object[] { "./TestConfigurations/configuration-invalid-fhirpath.json" };
            yield return new object[] { "./TestConfigurations/configuration-invalid-encryptkey.json" };
            yield return new object[] { "./TestConfigurations/configuration-miss-replacement.json" };
            yield return new object[] { "./TestConfigurations/configuration-perturb-wrong-rangetype.json" };
            yield return new object[] { "./TestConfigurations/configuration-perturb-miss-span.json" };
            yield return new object[] { "./TestConfigurations/configuration-perturb-negative-span.json" };
            yield return new object[] { "./TestConfigurations/configuration-perturb-wrong-roundTo.json" };
            yield return new object[] { "./TestConfigurations/configuration-perturb-negative-roundTo.json" };
            yield return new object[] { "./TestConfigurations/configuration-perturb-exceed-28-roundTo.json" };
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
