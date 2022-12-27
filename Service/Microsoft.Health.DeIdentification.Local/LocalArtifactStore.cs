using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Local
{
    public class LocalArtifactStore : IArtifactStore
    {

        public LocalArtifactStore() 
        {
        }

        public TContent ResolveArtifact<TContent>(string reference)
        {

            try
            {
                var content = File.ReadAllText(reference);
                return (TContent)(object)content;
            }
            catch(Exception innerException)
            {
                throw new Exception($"Failed to resolve artifact {reference}", innerException);
            }
        }
    }
}
