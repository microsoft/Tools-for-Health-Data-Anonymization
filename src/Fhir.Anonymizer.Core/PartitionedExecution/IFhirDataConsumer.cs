using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core
{
    public interface IFhirDataConsumer
    {
        Task<int> ConsumeAsync(IEnumerable<string> data);

        Task CompleteAsync();
    }
}
