namespace Fhir.Anonymizer.Core.Models
{
    public class ProcessResult
    {
        public AnonymizationSummary Summary { get; set; }

        public ProcessResult()
        {
            Summary = new AnonymizationSummary();
        }
    }
}
