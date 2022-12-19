using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdOperationProvider : IDeIdOperationProvider
    {
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdConfiguration deIdRuleSet)
        {
            // return null for format mismatch
            throw new NotImplementedException();
        }

        public IDeIdOperation<TSource, TResult> CreateDeIdOperationFromJson<TSource, TResult>(string jsonPath)
        {
            throw new NotImplementedException();
        }
    }
}
