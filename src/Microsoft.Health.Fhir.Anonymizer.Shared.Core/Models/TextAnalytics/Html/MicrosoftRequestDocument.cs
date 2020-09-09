using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html
{
    [DataContract]
    public class MicrosoftRequestDocument
    {
        [DataMember(Name = "id")]
        public string DocumentId { get; set; }

        [DataMember(Name = "language")]
        public string Language { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }
    }
}