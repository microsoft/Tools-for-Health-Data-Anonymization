namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics
{
    public class Entity
    {
        public string Category { get; set; }

        public string SubCategory { get; set; }

        public string Text { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }

        public double ConfidenceScore { get; set; }
    }
}
