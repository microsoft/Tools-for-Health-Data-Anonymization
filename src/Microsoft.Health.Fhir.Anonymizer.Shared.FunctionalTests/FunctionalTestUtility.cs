using System;
using System.IO;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.FunctionalTests
{
    public static class FunctionalTestUtility
    {
        public static void VerifySingleJsonResourceFromFile(AnonymizerEngine engine, string testFile, string targetFile)
        {
            Console.WriteLine($"VerifySingleJsonResourceFromFile. TestFile: {testFile}, TargetFile: {targetFile}");
            string testContent = File.ReadAllText(testFile);
            string targetContent = File.ReadAllText(targetFile);
            string resultContent = engine.AnonymizeJson(testContent);

            var targetObject = JObject.Parse(Standardize(targetContent));
            var resultObject = JObject.Parse(Standardize(resultContent));
            Assert.True(JToken.DeepEquals(targetObject, resultObject));
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
