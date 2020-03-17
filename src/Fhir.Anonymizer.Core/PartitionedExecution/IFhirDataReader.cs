using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core
{
    public interface IFhirDataReader
    {
        Task<string> NextAsync();
    }
}
