using Hl7.Fhir.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web.Models
{
    public class ResourceList
    {
        [JsonProperty("Resources")]
        public List<Object> Resources { get; set; }
    }
}
