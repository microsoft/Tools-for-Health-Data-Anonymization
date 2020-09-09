using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftResponseInnerError
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "message")]
        public string Massage { get; set; }
    }
}
