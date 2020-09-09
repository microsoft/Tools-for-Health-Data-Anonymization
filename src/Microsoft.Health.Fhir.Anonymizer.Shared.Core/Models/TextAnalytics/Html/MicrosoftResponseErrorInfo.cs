using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftResponseErrorInfo
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "message")]
        public string Massage { get; set; }

        [DataMember(Name = "innererror")]
        public MicrosoftResponseInnerError InnerError { get; set; }
    }
}
