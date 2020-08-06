using System.Threading.Tasks;

namespace MicrosoftFhir.Anonymizer.Core
{
    public interface IFhirDataReader<T>
    {
        Task<T> NextAsync();
    }
}
