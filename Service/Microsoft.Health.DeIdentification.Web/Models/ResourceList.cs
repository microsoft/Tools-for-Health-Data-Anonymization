using Newtonsoft.Json;
using System.Collections;

namespace Microsoft.Health.DeIdentification.Web.Models
{
    public class ResourceList
    {
        [JsonProperty("Resources")]
        public IList Resources { get; set; }
    }
}
