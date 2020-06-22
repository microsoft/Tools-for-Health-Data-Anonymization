using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core.Processors
{
    public class KeepProcessor: IAnonymizerProcessor
    {
        public async Task<ProcessResult> Process(ElementNode node)
        {
            return new ProcessResult();
        }
    }
}
