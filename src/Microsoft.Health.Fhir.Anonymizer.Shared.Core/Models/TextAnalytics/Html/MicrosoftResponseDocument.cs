using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftResponseDocument
    {
        [DataMember(Name = "id")]
        public string DocumentId { get; set; }

        [DataMember(Name = "entities")]
        public List<MicrosoftEntity> Entities { get; set; }

        [DataMember(Name = "warnings")]
        public List<string> Warnings { get; set; }
    }
}
