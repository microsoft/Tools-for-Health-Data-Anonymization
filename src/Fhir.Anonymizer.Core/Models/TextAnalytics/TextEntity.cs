using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fhir.Anonymizer.Core.Models.TextAnalytics
{
    [DataContract]
    public class TextEntity
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; }

        [DataMember(Name = "offset")]
        public int Offset { get; set; }

        [DataMember(Name = "length")]
        public int Length { get; set; }

        [DataMember(Name = "confidenceScore")]
        public double ConfidenceScore { get; set; }
    }
}