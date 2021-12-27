using System.Threading.Tasks;

namespace Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution
{
    public interface IFhirDataReader<T>
    {
        Task<T> NextAsync();
    }
}
