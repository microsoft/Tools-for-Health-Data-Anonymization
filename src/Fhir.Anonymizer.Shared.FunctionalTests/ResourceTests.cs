using System;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.FhirPath;
using Xunit;


namespace Fhir.Anonymizer.FunctionalTests
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

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
