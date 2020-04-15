namespace Fhir.Anonymizer.Core.Models
{
    public class ProcessResult
    {
        public bool IsRedacted { get; set; }

        public bool IsAbstracted { get; set; }

        public bool IsPerturbed { get; set; }

        public void Update(ProcessResult result)
        {
            IsRedacted |= result.IsRedacted;
            IsPerturbed |= result.IsPerturbed;
            IsAbstracted |= result.IsAbstracted;
        }
    }
}
