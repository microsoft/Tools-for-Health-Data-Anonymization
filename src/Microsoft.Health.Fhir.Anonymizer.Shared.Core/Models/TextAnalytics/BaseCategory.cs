using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics
{
    [DataContract]
    public class BaseCategory
    {
        [DataMember(Name = "category")]
        public string Category { get; set; }

        [DataMember(Name = "subcategory")]
        public string Subcategory { get; set; }
    }
}
