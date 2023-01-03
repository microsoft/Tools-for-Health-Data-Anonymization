// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Batch.Models;
using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Fhir.Models
{
    [DataContract]
    public class BatchFhirDeIdJobResult
    {
        [DataMember(Name = "metadata")]
        public BatchJobMetadata Metadata { get; set; } = new BatchJobMetadata();

        // TODO: the output should not larger than azure table entity 
        [DataMember(Name = "outputs")]
        public List<OutputInfo> Outputs { get; set; } = new List<OutputInfo>();
    }
}
