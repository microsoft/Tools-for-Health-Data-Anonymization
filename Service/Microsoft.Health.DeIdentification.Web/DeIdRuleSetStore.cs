using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Web
{
    public class DeIdRuleSetStore : IDeIdRuleSetStore
    {
        private IArtifactStore _artifactStore;

        public DeIdRuleSetStore(IArtifactStore artifactStore /* web service configuration */)
        {
            _artifactStore = artifactStore;
        }

        // Possible to have minutes level cache for further improvement
        public DeIdRuleSet GetByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
