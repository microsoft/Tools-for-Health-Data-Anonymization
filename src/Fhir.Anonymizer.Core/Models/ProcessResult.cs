namespace Fhir.Anonymizer.Core.Models
{
    public class ProcessResult
    {
        public bool IsRedacted { get; set; }

        public bool IsAbstracted { get; set; }

        public bool IsPerturbed { get; set; }

        public void UpdateResult(ProcessResult result)
        {
            IsRedacted = result.IsRedacted ? result.IsRedacted : IsRedacted;
            IsAbstracted = result.IsAbstracted ? result.IsAbstracted : IsAbstracted;
            IsPerturbed = result.IsPerturbed ? result.IsPerturbed : IsPerturbed;
        }
    }
}
