using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperation : IDeIdOperation<IList, string>
    {
        private AnonymizerEngine _anonymizerEngine;

        public FhirDeIdOperation(string configContext)
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            _anonymizerEngine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromSettingsInJson(configContext));
        }

        public string Process(IList source)
        {
            var result = new StringBuilder();
            foreach (var item in source)
            {
                result.AppendLine(ProcessSingle(item.ToString()));
            }
            return result.ToString();
        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }
    }
}