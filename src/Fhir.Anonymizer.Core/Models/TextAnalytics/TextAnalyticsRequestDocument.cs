using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fhir.Anonymizer.Core.Models.TextAnalytics
{
    [DataContract]
    public class TextAnalyticsRequestDocument
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "language")]
        public string Language { get; set; }
        [DataMember(Name = "text")]
        public string Text { get; set; }
    }
}
