namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IArtifactStore
    {
        public TContent ResolveArtifact<TContent>(string reference);
    }
}