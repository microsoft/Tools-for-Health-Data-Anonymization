using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperation : IDeIdOperation<string, string>
    {
        private AnonymizerEngine _anonymizerEngine;

        public FhirDeIdOperation(string configFilePath)
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            _anonymizerEngine = new AnonymizerEngine(configFilePath);
        }

        public string Process(string source)
        {
            return _anonymizerEngine.AnonymizeJson(source);
        }
    }
}