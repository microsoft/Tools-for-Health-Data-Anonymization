namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdOperationProvider
    {
        public IList<IDeIdOperation<TSource, TResult>> CreateDeIdOperations<TSource, TResult>(DeIdConfiguration deIdConfiguration);
    }
}
