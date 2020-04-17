using System.IO;
using Fhir.Anonymizer.Core;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class ResourceTests
    {
        private AnonymizerEngine _engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));

        [Fact]
        public void GivenAPatientResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(_engine, ResourceTestsFile("patient-basic.json"), ResourceTestsFile("patient-basic-target.json"));
        }

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
