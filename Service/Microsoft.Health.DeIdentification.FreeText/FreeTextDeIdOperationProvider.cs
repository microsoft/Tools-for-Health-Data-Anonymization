using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdOperationProvider : IDeIdOperationProvider
    {
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdRuleSet deIdRuleSet)
        {
            // return null for format mismatch
            throw new NotImplementedException();
        }
    }
}
