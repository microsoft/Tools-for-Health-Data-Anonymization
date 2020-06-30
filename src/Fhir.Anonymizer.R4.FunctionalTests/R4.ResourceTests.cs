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
            yield return new object[] { "Stu3OnlyResource/DeviceComponent","DeviceComponent" };
            yield return new object[] { "Stu3OnlyResource/ProcessRequest","ProcessRequest" };
            yield return new object[] { "Stu3OnlyResource/ProcessResponse","ProcessResponse" };
        }

        public static IEnumerable<object[]> GetR4OnlyResources()
        {
            yield return new object[] { "R4OnlyResource/Organizationaffiliation", "R4OnlyResource/Organizationaffiliation-target" };
            yield return new object[] { "R4OnlyResource/MedicinalProduct", "R4OnlyResource/MedicinalProduct-target" };
            yield return new object[] { "R4OnlyResource/ServiceRequest", "R4OnlyResource/ServiceRequest-target" };
        }

        public static IEnumerable<object[]> GetCommonResourcesWithStu3OnlyField()
        {
            yield return new object[] { "Stu3OnlyResource/Claim-Stu3"};
            yield return new object[] { "Stu3OnlyResource/Account-Stu3" };
        }

        public static IEnumerable<object[]> GetCommonResourcesWithR4OnlyField()
        {
            yield return new object[] { "R4OnlyResource/Claim-R4", "R4OnlyResource/Claim-R4-target" };
            yield return new object[] { "R4OnlyResource/Account-R4", "R4OnlyResource/Account-R4-target" };
        }

        [Theory]
        [MemberData(nameof(GetStu3OnlyResources))]
      
        public void GivenAStu3OnlyResource_WhenAnonymizing_ExceptionShouldBeReturned(string testFile,string ResourceName)
        {

            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "r4-configuration-sample.json"));
            string testContent = File.ReadAllText(ResourceTestsFile(testFile));
            var ex = Assert.Throws<FormatException>(() => engine.AnonymizeJson(testContent));
            var expectedError = "type (at Cannot locate type information for type '"+ ResourceName + "')";
            Assert.Equal(expectedError, ex.Message.ToString());
           
        }

        [Theory]
        [MemberData(nameof(GetR4OnlyResources))]
        public void GivenAR4OnlyResource_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile,string targetFile)
        {
 
            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "r4-configuration-sample.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile(testFile), ResourceTestsFile(targetFile));
        }


        [Theory]
        [MemberData(nameof(GetCommonResourcesWithR4OnlyField))]

        public void GivenCommonResourceWithR4OnlyField_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile, string targetFile)
        {

            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "r4-configuration-sample.json"));
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile(testFile), ResourceTestsFile(targetFile));


        }

        [Theory]
        [MemberData(nameof(GetCommonResourcesWithStu3OnlyField))]

        public void GivenCommonResourceWithStu3OnlyField_WhenAnonymizing_ExceptionShouldBeReturned(string testFile)
        {

            AnonymizerEngine engine = new AnonymizerEngine(Path.Combine("Configurations", "r4-configuration-sample.json"));
            string testContent = File.ReadAllText(ResourceTestsFile(testFile));
            Assert.Throws<FormatException>(() => engine.AnonymizeJson(testContent));
            

        }

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("../../../../Fhir.Anonymizer.Shared.FunctionalTests/TestResources", fileName+".json");
        }

    }
}
