using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Azure
{
    public class AzureBlobArtifactStore : IArtifactStore
    {
        public TContent ResolveArtifact<TContent>(string reference)
        {
            throw new NotImplementedException();
        }
    }
}