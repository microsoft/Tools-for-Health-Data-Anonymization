using System;
using System.Collections.Generic;
using System.IO;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.FhirPath;
using Xunit;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;

namespace Fhir.Anonymizer.FunctionalTests
{
    public class VersionSpecificTests
    {
        public VersionSpecificTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }
        public static IEnumerable<object[]> GetStu3OnlyResources()
        {
            yield return new object[] { "Stu3OnlyResource/DeviceComponent", "Stu3OnlyResource/DeviceComponent-target" };
            yield return new object[] { "Stu3OnlyResource/ProcessRequest", "Stu3OnlyResource/ProcessRequest-target" };
            yield return new object[] { "Stu3OnlyResource/ProcessResponse", "Stu3OnlyResource/ProcessResponse-target" };
        }

        public static IEnumerable<object[]> GetR4OnlyResources()
        {
            yield return new object[] { "R4OnlyResource/OrganizationAffiliation", "OrganizationAffiliation" };
            yield return new object[] { "R4OnlyResource/MedicinalProduct", "MedicinalProduct" };
            yield return new object[] { "R4OnlyResource/ServiceRequest", "ServiceRequest" };
        }

        public static IEnumerable<object[]> GetCommonResourcesWithStu3OnlyField()
        {
            yield return new object[] { "Stu3OnlyResource/Claim-Stu3", "Stu3OnlyResource/Claim-Stu3-target" };
            yield return new object[] { "Stu3OnlyResource/Account-Stu3", "Stu3OnlyResource/Account-Stu3-target" };
            yield return new object[] { "Stu3OnlyResource/Contract-Stu3", "Stu3OnlyResource/Contract-Stu3-target" };
        }

        public static IEnumerable<object[]> GetCommonResourcesWithR4OnlyField()
        {
            yield return new object[] { "R4OnlyResource/Claim-R4" };
            yield return new object[] { "R4OnlyResource/Account-R4" };
            yield return new object[] { "R4OnlyResource/Contract-R4" };
        }   

        [Theory]
        [MemberData(nameof(GetR4OnlyResources))]

        public void GivenAR4OnlyResource_WhenAnonymizing_ExceptionShouldBeReturned(string testFile, string ResourceName)
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "stu3-configuration-sample.json"));
            string testContent = File.ReadAllText(ResourceTestsFile(testFile));
            var ex = Assert.Throws<FormatException>(() => engine.AnonymizeJson(testContent));
            var expectedError = "type (at Cannot locate type information for type '" + ResourceName + "')";
            Assert.Equal(expectedError, ex.Message.ToString());
        }

        [Theory]
        [MemberData(nameof(GetStu3OnlyResources))]
        public void GivenAStu3OnlyResource_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile, string targetFile)
        {
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "stu3-configuration-sample.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile(testFile), ResourceTestsFile(targetFile));
        }

        [Theory]
        [MemberData(nameof(GetCommonResourcesWithStu3OnlyField))]

        public void GivenCommonResourceWithStu3OnlyField_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile, string targetFile)
        {

            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "stu3-configuration-sample.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile(testFile), ResourceTestsFile(targetFile));   
        }

        [Theory]
        [MemberData(nameof(GetCommonResourcesWithR4OnlyField))]

        public void GivenCommonResourceWithR4OnlyField_WhenAnonymizing_ExceptionShouldBeReturned(string testFile)
        { 
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "stu3-configuration-sample.json"));
            string testContent = File.ReadAllText(ResourceTestsFile(testFile));
            Assert.Throws<FormatException>(() => engine.AnonymizeJson(testContent));
        }
        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("../../../../Fhir.Anonymizer.Shared.FunctionalTests/TestResources", fileName + ".json");
        }

    }
}
