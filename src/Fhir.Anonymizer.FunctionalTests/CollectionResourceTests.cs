using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.FhirPath;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class CollectionResourceTests
    {
        public CollectionResourceTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        [Fact]
        public void GivenAResourceWithContained_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-basic.json"), CollectionResourceTestsFile("contained-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResourceWithContainedInside_WhenAnonymizing_ThenContainedResourceShouldBeAnonymized()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-in-bundle.json"), CollectionResourceTestsFile("contained-in-bundle-target.json"));
        }

        [Fact]
        public void GivenAResourceWithContained_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-basic.json"), CollectionResourceTestsFile("contained-redact-all-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-redact-all-target.json"));
        }

        [Fact]
        public void GivenABundleResourceWithContainedInside_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-in-bundle.json"), CollectionResourceTestsFile("contained-in-bundle-redact-all-target.json"));
        }

        private string CollectionResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
