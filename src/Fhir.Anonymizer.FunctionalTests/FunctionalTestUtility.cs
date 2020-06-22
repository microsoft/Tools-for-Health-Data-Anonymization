using System;
using System.IO;
using Fhir.Anonymizer.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Fhir.Anonymizer.FunctionalTests
{
    public static class FunctionalTestUtility
    {
        public static async void VerifySingleJsonResourceFromFile(AnonymizerEngine engine, string testFile, string targetFile)
        {
            Console.WriteLine($"VerifySingleJsonResourceFromFile. TestFile: {testFile}, TargetFile: {targetFile}");
            string testContent = File.ReadAllText(testFile);
            string targetContent = File.ReadAllText(targetFile);
            string resultAfterAnonymize = await engine.AnonymizeJson(testContent);

            Assert.Equal(Standardize(targetContent), Standardize(resultAfterAnonymize));
        } 

        private static string Standardize(string jsonContent)
        {
            var resource = new FhirJsonParser().Parse<Resource>(jsonContent);
            FhirJsonSerializationSettings serializationSettings = new FhirJsonSerializationSettings
            {
                Pretty = true
            };
            return resource.ToJson(serializationSettings);
        }
    }
}
