using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Newtonsoft.Json;
using System.Collections;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperation : IDeIdOperation<IList, IList>
    {
        private AnonymizerEngine _anonymizerEngine;

        public FhirDeIdOperation(string configContext)
        {
            AnonymizerEngine.InitializeFhirPathExtensionSymbols();
            _anonymizerEngine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromSettingsInJson(configContext));
        }

        public IList Process(IList source)
        {
            var result = new List<object>();
            foreach (var item in source)
            {
                result.Add(JsonConvert.DeserializeObject(ProcessSingle(item.ToString())));
            }
            return result;
        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }
    }
}