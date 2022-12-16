using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperationProvider : IDeIdOperationProvider
    {
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdRuleSet deIdRuleSet)
        {
            throw new NotImplementedException();
        }
    }
}