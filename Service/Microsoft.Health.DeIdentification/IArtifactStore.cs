namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IArtifactStore
    {
        public string DefaultConfigFile { get; }

        public TContent ResolveArtifact<TContent>(string reference);
    }
}