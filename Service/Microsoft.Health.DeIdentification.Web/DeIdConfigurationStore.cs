using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Web
{
    public class DeIdConfigurationStore : IDeIdConfigurationStore
    {
        private IArtifactStore _artifactStore;

        public DeIdConfigurationStore(IArtifactStore artifactStore /* web service configuration */)
        {
            _artifactStore = artifactStore;
        }

        // Possible to have minutes level cache for further improvement
        public DeIdConfiguration GetByName(string name)
        {
            return _artifactStore.ResolveArtifact<DeIdConfiguration>(name);
        }
    }
}
