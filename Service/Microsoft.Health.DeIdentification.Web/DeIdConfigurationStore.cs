using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Web
{
    public class DeIdConfigurationStore : IDeIdConfigurationStore
    {
        private IArtifactStore _artifactStore;
        private Dictionary<string, DeIdConfiguration> _deIdConfigurations;

        public DeIdConfigurationStore(IArtifactStore artifactStore /* web service configuration */)
        {
            _artifactStore = artifactStore;

            // Load default configurations
            // TODO: check configuration name unique
            // lazy initialization

            _deIdConfigurations =_artifactStore.ResolveArtifact<Configuration>(_artifactStore.DefaultConfigFile).DeidConfigurations.ToDictionary(x => x.Name, x => x);
        }

        // Possible to have minutes level cache for further improvement
        public DeIdConfiguration GetByName(string name)
        {
            if (_deIdConfigurations.ContainsKey(name))
            {
                return _deIdConfigurations[name];
            }
            else
            {
                throw new Exception($"The configuration {name} does not exist.");
            }
        }
    }
}
