namespace Fhir.Anonymizer.Core.PartitionedExecution
{
    public class BatchAnonymizeResult
    {
        public int Complete { get; set; }

        public int Failed { get; set; }
    }
}
