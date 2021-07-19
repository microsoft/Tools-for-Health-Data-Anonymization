﻿namespace Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution
{
    public class BatchAnonymizeProgressDetail
    {
        public int CurrentThreadId { get; set; }

        public int ProcessCompleted { get; set; }

        public int ProcessSkipped { get; set; }

        public int ConsumeCompleted { get; set; }
    }
}
