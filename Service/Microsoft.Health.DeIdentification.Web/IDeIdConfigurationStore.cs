namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdConfigurationStore
    {
        public DeIdConfiguration GetByName(string name);
    }
}
