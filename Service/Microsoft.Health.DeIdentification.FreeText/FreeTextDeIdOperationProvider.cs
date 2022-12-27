using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdOperationProvider : IDeIdOperationProvider
    {
        public IList<IDeIdOperation<TSource, TResult>> CreateDeIdOperations<TSource, TResult>(DeIdConfiguration deIdConfiguration)
        {
            // return null for format mismatch
            throw new NotImplementedException();
        }
    }
}
