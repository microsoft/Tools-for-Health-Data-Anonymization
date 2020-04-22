using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class ResourceTests
    {
        private AnonymizerEngine engine;
        public ResourceTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-basic.json"), ResourceTestsFile("patient-basic-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-basic.json"), ResourceTestsFile("patient-redact-all-target.json"));
        }

        [Fact]
        public void GivenAPatientResourceWithSpecialContents_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-special-content.json"), ResourceTestsFile("patient-special-content-target.json"));
        }

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
