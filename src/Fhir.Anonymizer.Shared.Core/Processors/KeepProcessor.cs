using System.Collections.Generic;
using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public class KeepProcessor: IAnonymizerProcessor
    {
        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            return new ProcessResult();
        }
    }
}
