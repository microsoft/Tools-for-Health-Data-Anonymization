using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftResponseContent
    {
        [DataMember(Name = "documents")]
        public List<MicrosoftResponseDocument> Documents { get; set; }

        [DataMember(Name = "errors")]
        public List<MicrosoftResponseError> Errors { get; set; }

        [DataMember(Name = "modelVersion")]
        public string ModelVersion { get; set; }
    }
}
