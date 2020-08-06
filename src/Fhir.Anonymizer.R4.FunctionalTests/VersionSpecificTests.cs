using System;
using System.Collections.Generic;
using System.IO;
using MicrosoftFhir.Anonymizer.Core;
using MicrosoftFhir.Anonymizer.Core.Extensions;
using Hl7.FhirPath;
using Xunit;

namespace MicrosoftFhir.Anonymizer.FunctionalTests
{
    public class VersionSpecificTests
    {
        public VersionSpecificTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public static IEnumerable<object[]> GetStu3OnlyResources()
        {
            yield return new object[] { "Stu3OnlyResource/DeviceComponent.json","DeviceComponent" };
            yield return new object[] { "Stu3OnlyResource/ProcessRequest.json", "ProcessRequest" };
            yield return new object[] { "Stu3OnlyResource/ProcessResponse.json", "ProcessResponse" };
        }

        public static IEnumerable<object[]> GetR4OnlyResources()
        {
            yield return new object[] { "R4OnlyResource/Organizationaffiliation.json", "R4OnlyResource/Organizationaffiliation-target.json" };
            yield return new object[] { "R4OnlyResource/MedicinalProduct.json", "R4OnlyResource/MedicinalProduct-target.json" };
            yield return new object[] { "R4OnlyResource/ServiceRequest.json", "R4OnlyResource/ServiceRequest-target.json" };
        }

        public static IEnumerable<object[]> GetCommonResourcesWithStu3OnlyField()
        {
            yield return new object[] { "Stu3OnlyResource/Claim-Stu3.json" };
            yield return new object[] { "Stu3OnlyResource/Account-Stu3.json" };
            yield return new object[] { "Stu3OnlyResource/Contract-Stu3.json" };
        }

        public static IEnumerable<object[]> GetCommonResourcesWithR4OnlyField()
        {
            yield return new object[] { "R4OnlyResource/Claim-R4.json", "R4OnlyResource/Claim-R4-target.json" };
            yield return new object[] { "R4OnlyResource/Account-R4.json", "R4OnlyResource/Account-R4-target.json" };
            yield return new object[] { "R4OnlyResource/Contract-R4.json", "R4OnlyResource/Contract-R4-target.json" };
        }

        [Theory]
        [MemberData(nameof(GetStu3OnlyResources))]
        public void GivenAStu3OnlyResource_WhenAnonymizing_ExceptionShouldBeThrown(string testFile, string ResourceName)
        {
            AnonymizerEngine engine = new AnonymizerEngine("r4-configuration-sample.json");
            string testContent = File.ReadAllText(ResourceTestsFile(testFile));
            var ex = Assert.Throws<FormatException>(() => engine.AnonymizeJson(testContent));
            var expectedError = "type (at Cannot locate type information for type '"+ ResourceName + "')";
            
            Assert.Equal(expectedError, ex.Message.ToString());
        }

        [Theory]
        [MemberData(nameof(GetR4OnlyResources))]
        public void GivenAR4OnlyResource_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile, string targetFile)
        {
            AnonymizerEngine engine = new AnonymizerEngine("r4-configuration-sample.json");
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile(testFile), ResourceTestsFile(targetFile));
        }

        [Theory]
        [MemberData(nameof(GetCommonResourcesWithR4OnlyField))]
        public void GivenCommonResourceWithR4OnlyField_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile, string targetFile)
        {
            AnonymizerEngine engine = new AnonymizerEngine("r4-configuration-sample.json");
            FunctionalTestUtility.VerifySingleJsonResourceFromFile(engine, ResourceTestsFile(testFile), ResourceTestsFile(targetFile)); 
        }

        [Theory]
        [MemberData(nameof(GetCommonResourcesWithStu3OnlyField))]
        public void GivenCommonResourceWithStu3OnlyField_WhenAnonymizing_ExceptionShouldBeThrown(string testFile)
        {
            AnonymizerEngine engine = new AnonymizerEngine("r4-configuration-sample.json");
            string testContent = File.ReadAllText(ResourceTestsFile(testFile));
            
            Assert.Throws<FormatException>(() => engine.AnonymizeJson(testContent));
        }

        private string ResourceTestsFile(string fileName)
        {
            return Path.Combine("TestResources", fileName);
        }
    }
}
