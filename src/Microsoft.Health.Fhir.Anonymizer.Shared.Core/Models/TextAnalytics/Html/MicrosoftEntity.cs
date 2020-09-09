using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftEntity
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; }

        [DataMember(Name = "subcategory")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("")]
        public string SubCategory { get; set; }

        [DataMember(Name = "offset")]
        public int Offset { get; set; }

        [DataMember(Name = "length")]
        public int Length { get; set; }

        [DataMember(Name = "confidenceScore")]
        public double ConfidenceScore { get; set; }
    }
}
