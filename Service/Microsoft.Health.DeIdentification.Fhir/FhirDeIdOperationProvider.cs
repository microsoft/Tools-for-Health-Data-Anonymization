using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperationProvider : IDeIdOperationProvider
    {
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdConfiguration deIdConfiguration)
        {
            throw new NotImplementedException();
        }

        public IDeIdOperation<TSource, TResult> CreateDeIdOperationFromJson<TSource, TResult>(string jsonPath)
        {
            var operation = new FhirDeIdOperation(jsonPath);
            return (IDeIdOperation<TSource, TResult>)operation;
        }
    }
}