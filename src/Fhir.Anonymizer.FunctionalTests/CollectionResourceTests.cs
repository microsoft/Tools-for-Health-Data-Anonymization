using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class CollectionResourceTests
    {
        private AnonymizerEngine _engine;
        public CollectionResourceTests()
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
        public void GivenAResourceWithContained_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(_engine, CollectionResourceTestsFile("contained-basic.json"), CollectionResourceTestsFile("contained-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(_engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResourceWithContainedInside_WhenAnonymizing_ThenContainedResourceShouldBeAnonymized()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(_engine, CollectionResourceTestsFile("contained-in-bundle.json"), CollectionResourceTestsFile("contained-in-bundle-target.json"));
        }

        private string CollectionResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
