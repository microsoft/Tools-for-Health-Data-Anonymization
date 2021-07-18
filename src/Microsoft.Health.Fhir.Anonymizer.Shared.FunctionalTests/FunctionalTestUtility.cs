using System;
using System.IO;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Newtonsoft.Json;
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
            //string resultAfterAnonymize = engine.AnonymizeJson(testContent);

            IStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();
            var _parser = new FhirJsonParser();
            var resource = _parser.Parse<Resource>(testContent);
            var elements = engine.AnonymizeElement(resource.ToTypedElement());
            var string1 = elements.ToJson();
            var string2 = elements.ToPoco<Resource>().ToJson();
            Assert.Equal(string1, string2);
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
