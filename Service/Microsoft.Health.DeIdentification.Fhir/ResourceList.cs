using Hl7.Fhir.Model;
using Newtonsoft.Json;
using System.Collections;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class ResourceList
    {
        [JsonProperty("Resources")]
        public IList Resources { get; set; }
    }
}
