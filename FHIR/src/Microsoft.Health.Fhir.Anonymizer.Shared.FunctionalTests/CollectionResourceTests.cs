﻿using System.IO;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.FunctionalTests
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
        public void GivenAResourceWithContained_WhenAnonymizingWithBadFile_IfIgnore_EmptyResultWillBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "configuration-ignoreinvalid-processing-error.json"));
            FunctionalTestUtility.VerifyEmptyStringFromFile(engine, CollectionResourceTestsFile("invalid-resource.json"));
        }

        [Fact]
        public void GivenAResourceWithContained_WhenAnonymizingWithBadJsonFile_IfIgnore_EmptyResultWillBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "configuration-ignoreinvalid-processing-error.json"));
            FunctionalTestUtility.VerifyEmptyStringFromFile(engine, CollectionResourceTestsFile("invalid-json-resource.json"));
        }

        [Fact]
        public void GivenAResourceWithContained_WhenAnonymizingWithProcessingError_IfSkip_EmptyResultWillBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "configuration-skip-processing-error.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-basic.json"), CollectionResourceTestsFile("condition-empty.json"));
        }

        [Fact]
        public void GivenAResourceWithContained_WhenAnonymizingWithProcessingError_IfRaise_ExceptionWillBeThrown()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "configuration-raise-processing-error.json"));
            string testContent = File.ReadAllText(CollectionResourceTestsFile("contained-basic.json"));
            Assert.Throws<AnonymizerProcessingException>(() => engine.AnonymizeJson(testContent));
        }

        [Fact]
        public void GivenABundleResource_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-basic-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenAnonymizingWithProcessingError_IfSkip_EmptyResultWillBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "configuration-skip-processing-error.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-empty.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenAnonymizingWithProcessingError_IfRaise_ExceptionWillBeThrown()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "configuration-raise-processing-error.json"));
            string testContent = File.ReadAllText(CollectionResourceTestsFile("bundle-basic.json"));
            Assert.Throws<AnonymizerProcessingException>(() => engine.AnonymizeJson(testContent));
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
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-basic.json"), CollectionResourceTestsFile("contained-redact-all-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-basic.json"), CollectionResourceTestsFile("bundle-redact-all-target.json"));
        }

        [Fact]
        public void GivenABundleResourceWithContainedInside_WhenRedactAll_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-in-bundle.json"), CollectionResourceTestsFile("contained-in-bundle-redact-all-target.json"));
        }

        [Fact]
        public void GivenAResourceWithContained_WhenSubstitute_ThenRedactedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-multiple.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("contained-substitute.json"), CollectionResourceTestsFile("contained-substitute-target.json"));
        }

        [Fact]
        public void GivenABundleResource_WhenSubstitute_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-multiple.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, CollectionResourceTestsFile("bundle-substitute.json"), CollectionResourceTestsFile("bundle-substitute-target.json"));
        }

        private string CollectionResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
