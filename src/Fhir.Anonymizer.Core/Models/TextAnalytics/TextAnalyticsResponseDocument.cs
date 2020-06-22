using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Fhir.Anonymizer.Core.Models.TextAnalytics
{
    [DataContract]
    public class TextAnalyticsResponseDocument
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "entities")]
        public IEnumerable<TextEntity> Entities { get; set; }
    }
}
