using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core.Utility.NamedEntityRecognition
{
    public interface INamedEntityRecognizer
    {
        Task<IEnumerable<string>> AnonymizeText(IEnumerable<string> textList);
    }
}
