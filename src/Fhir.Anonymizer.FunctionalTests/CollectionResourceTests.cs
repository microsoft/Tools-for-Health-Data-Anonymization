using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.ResourceTransformers;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class CollectionResourceTests
    {
        private AnonymizerEngine _engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));

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
