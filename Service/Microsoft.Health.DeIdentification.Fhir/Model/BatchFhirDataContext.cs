// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch.Models.Data;
using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Fhir.Model
{
    [DataContract]
    public class BatchFhirDataContext
    {
        [DataMember(Name = "resources")]
        public StringBatchData Resources { get; set; }

        [DataMember(Name = "inputFileName")]
        public string InputFileName { get; set; }

        [DataMember(Name = "outputFileName")]
        public string OutputFileName { get; set; }
    }
}
