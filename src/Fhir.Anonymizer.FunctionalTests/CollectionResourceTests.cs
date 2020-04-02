using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Resource;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class CollectionResourceTests
    {
        private AnonymizerEngine engine;
        public CollectionResourceTests()
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
        public void GivenAResourceWithContained_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-basic.json"), CollectionResourceTestsFile("contained-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResourceWithContainedInside_WhenAnonymizing_ThenContainedResourceShouldBeAnonymized()
        {
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-in-bundle.json"), CollectionResourceTestsFile("contained-in-bundle-target.json"));
        }

        private string CollectionResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
