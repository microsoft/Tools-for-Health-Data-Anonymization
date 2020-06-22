using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fhir.Anonymizer.Core.Models.TextAnalytics
{
    [DataContract]
    public class TextAnalyticsResponseContent
    {
        [DataMember(Name = "documents")]
        public IEnumerable<TextAnalyticsResponseDocument> Documents { get; set; }
    }
}
