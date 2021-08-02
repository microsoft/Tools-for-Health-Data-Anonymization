using System;
using System.IO;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.FunctionalTests
{
    public class ResourceTests
    {
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
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "redact-all-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-basic.json"), ResourceTestsFile("patient-redact-all-target.json"));
        }

        [Fact]
        public void GivenAPatientResourceWithSpecialContents_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-special-content.json"), ResourceTestsFile("patient-special-content-target.json"));
        }

        [Fact]
        public void GivenAPatientResourceWithNullDatetime_WhenAnonymizing_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-null-date.json"), ResourceTestsFile("patient-null-date-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizingWithNoPartialRedactConfig_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "common-no-partial-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-no-partial.json"), ResourceTestsFile("patient-no-partial-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizingWithPrimitiveSubstituteConfig_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-primitive.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-substitute-primitive.json"), ResourceTestsFile("patient-substitute-primitive-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizingWithComplexSubstituteConfig_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-complex.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-substitute-complex.json"), ResourceTestsFile("patient-substitute-complex-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizingWithConflictRulesSubstituteConfig_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-conflict-rules.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-substitute-conflict-rules.json"), ResourceTestsFile("patient-substitute-conflict-rules-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizingWithGeneralizeConfig_ThenAnonymizedJsonShouldBeReturned()
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "generalize-patient-config.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-generalize.json"), ResourceTestsFile("patient-generalize-target.json"));
        }

        [Fact]
        public void GivenAPatientResource_WhenAnonymizingWithMultipleSubstituteConfig_ThenAnonymizedJsonShouldBeReturned()
        {
            // Child node is substituted first
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-multiple.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-substitute-multiple.json"), ResourceTestsFile("patient-substitute-multiple-target.json"));
            // Parent node is substituted first
            engine = new AnonymizerEngine(Path.Combine("Configurations", "substitute-multiple-2.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile("patient-substitute-multiple-2.json"), ResourceTestsFile("patient-substitute-multiple-2-target.json"));
        }

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
