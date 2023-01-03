// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Batch.Models
{
    [DataContract]
    public class BatchJobMetadata
    {
        [DataMember(Name = "startTime")]
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

        [DataMember(Name = "completedTime")]
        public DateTimeOffset CompletedTime { get; set;}

        [DataMember(Name = "executionTimeInMS")]
        public double ExecutionTimeInMS { get; set; }

        [DataMember(Name = "fileCount")]
        public int FileCount { get; set; }

        //[DataMember(Name = "resoureCount")]
        //public int ResoureCount { get; set; }

        //[DataMember(Name = "reourceSize")]
        //public long ReourceSize { get; set; }
    }
}
