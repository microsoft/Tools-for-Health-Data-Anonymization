using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class ResourceTests
    {
        private AnonymizerEngine _engine;
        public ResourceTests()
        {
            var idTransformer = new ResourceIdTransformer();
            idTransformer.LoadExistingMapping(new Dictionary<string, string>
            {
                { "1", "1-abc" },
                { "23", "23-abc" },
                { "123", "123-abc" },
                { "bundle-references", "bundle-references-abc" },
                { "p1", "p1-abc" },
                { "example", "example-abc" },
            });

            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(Path.Combine("TestConfigurations", "configuration-test-sample.json"));
            _engine = new AnonymizerEngine(configurationManager, idTransformer);
        }

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
