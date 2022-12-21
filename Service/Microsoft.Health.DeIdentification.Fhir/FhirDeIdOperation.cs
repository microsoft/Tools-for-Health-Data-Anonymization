using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperation : IDeIdOperation<List<Object>, string>
    {
        private AnonymizerEngine _anonymizerEngine;

        public FhirDeIdOperation(string configPath)
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            _anonymizerEngine = new AnonymizerEngine(configPath);
        }

        public string Process(List<Object> source)
        {
            var result = new StringBuilder();
            foreach (var item in source)
            {
                result.Append(ProcessSingle(item.ToString()));
            }
            return result.ToString();
        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }
    }
}