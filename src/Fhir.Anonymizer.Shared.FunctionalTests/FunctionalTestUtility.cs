using System;
using System.IO;
using MicrosoftFhir.Anonymizer.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace MicrosoftFhir.Anonymizer.FunctionalTests
{
    public static class FunctionalTestUtility
    {
        public static void VerifySingleJsonResourceFromFile(AnonymizerEngine engine, string testFile, string targetFile)
        {
            Console.WriteLine($"VerifySingleJsonResourceFromFile. TestFile: {testFile}, TargetFile: {targetFile}");
            string testContent = File.ReadAllText(testFile);
            string targetContent = File.ReadAllText(targetFile);
            string resultAfterAnonymize = engine.AnonymizeJson(testContent);

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
