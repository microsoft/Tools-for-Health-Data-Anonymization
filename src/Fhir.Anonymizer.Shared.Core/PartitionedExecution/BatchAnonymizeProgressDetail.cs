namespace MicrosoftFhir.Anonymizer.Core.PartitionedExecution
{
    public class BatchAnonymizeProgressDetail
    {
        public int CurrentThreadId { get; set; }

        public int ProcessCompleted { get; set; }

        public int ProcessFailed { get; set; }

        public int ConsumeCompleted { get; set; }
    }
}
