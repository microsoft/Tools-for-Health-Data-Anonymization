using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftResponseError
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "error")]
        public MicrosoftResponseErrorInfo Error { get; set; }
    }
}
