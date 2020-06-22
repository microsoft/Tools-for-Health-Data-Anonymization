using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core.Processors
{
    public interface IAnonymizerProcessor
    {
        public Task<ProcessResult> Process(ElementNode node);
    }
}
