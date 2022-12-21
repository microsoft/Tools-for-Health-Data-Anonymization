using Microsoft.Health.DeIdentification.Contract;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.DeIdentification.Local
{
    public class LocalArtifactStore : IArtifactStore
    {

        readonly string _defaultConfigFile = "../Microsoft.Health.DeIdentification.Local/configurations/deid-configuration.json";

        public LocalArtifactStore() 
        {
        }

        public string DefaultConfigFile { get { return _defaultConfigFile;} }

        public TContent ResolveArtifact<TContent>(string reference)
        {
            try
            {
                var content = File.ReadAllText(reference);
                JsonLoadSettings settings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var token = JToken.Parse(content, settings);
                return token.ToObject<TContent>();
            }
            catch (IOException innerException)
            {
                throw new Exception($"Failed to read file {reference}", innerException);
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to parse json", ex);
            }
        }
    }
}
