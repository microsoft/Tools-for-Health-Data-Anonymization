namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics
{
    public class Segment
    {
        public int Offset { get; set; }
        public string Text { get; set; }
        public string DocumentId { get; set; }
    }
}
