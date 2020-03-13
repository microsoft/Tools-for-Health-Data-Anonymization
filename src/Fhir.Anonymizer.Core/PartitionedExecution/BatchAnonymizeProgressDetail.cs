namespace Fhir.Anonymizer.Core.PartitionedExecution
{
    public class BatchAnonymizeProgressDetail
    {
        public int Completed { get; set; }

        public int Failed { get; set; }
    }
}
