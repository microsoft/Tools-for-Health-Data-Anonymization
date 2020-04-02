using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Resource;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class ResourceTests
    {
        private AnonymizerEngine engine;
        public ResourceTests()
        {
            engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            ResourceIdTransformer.LoadExistingMapping(new Dictionary<string, string>
            {
                { "1", "1-abc" },
                { "23", "23-abc" },
                { "123", "123-abc" },
                { "bundle-references", "bundle-references-abc" },
                { "p1", "p1-abc" },
                { "example", "example-abc" },
            });
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-basic.json"), ResourceTestsFile("patient-basic-target.json"));
        }

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
