using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Shared.Core.Models;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public interface IAnonymizerProcessor
    {
        public ProcessResult Process(ElementNode node, ProcessSetting setting = null);
    }
}
