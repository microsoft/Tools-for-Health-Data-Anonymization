using Newtonsoft.Json;
using System.Collections;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class ResourceList
    {
        [JsonProperty("resources")]
        public IList Resources { get; set; }
    }
}
