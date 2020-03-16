namespace Fhir.Anonymizer.Core.PartitionedExecution
{
    public class BatchAnonymizeProgressDetail
    {
        public int ProcessCompleted { get; set; }

        public int ProcessFailed { get; set; }

        public int ConsumeCompleted { get; set; }
    }
}
