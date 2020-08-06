using System.Collections.Generic;
using MicrosoftFhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;

namespace MicrosoftFhir.Anonymizer.Core.Processors
{
    public interface IAnonymizerProcessor
    {
        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null);
    }
}
