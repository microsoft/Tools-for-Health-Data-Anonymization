using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Fhir.Models
{
    public class SourceDataset
    {
        [JsonProperty("format")]
        public string format { get; set; }

        [JsonProperty("url")]
        public string url { get; set; }
    }
}
