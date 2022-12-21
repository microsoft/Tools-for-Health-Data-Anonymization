namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdOperation<TSource, TResult>
    {
        public TResult Process(TSource source);

        public Task ProcessNdjson();
    }
}