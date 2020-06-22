using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fhir.Anonymizer.Core.Models.TextAnalytics
{
    [DataContract]
    public class TextAnalyticsRequestContent
    {
        [DataMember(Name = "documents")]
        public IEnumerable<TextAnalyticsRequestDocument> Documents { get; set; }
    }
}
