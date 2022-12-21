using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Azure
{
    public class AzureBlobArtifactStore : IArtifactStore
    {
        public string DefaultConfigFile => throw new NotImplementedException();

        public TContent ResolveArtifact<TContent>(string reference)
        {
            throw new NotImplementedException();
        }
    }
}