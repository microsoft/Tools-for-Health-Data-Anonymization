using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core
{
    public interface IFhirDataReader<T>
    {
        Task<T> NextAsync();
    }
}
