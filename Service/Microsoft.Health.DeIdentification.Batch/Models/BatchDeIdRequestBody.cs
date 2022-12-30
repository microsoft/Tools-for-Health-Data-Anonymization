// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Batch.Model
{
    [DataContract]
    public class BatchDeIdRequestBody
    {
        [DataMember(Name = "sourceDataset")]
        public SourceDataset SourceDataset { get; set; }

        [DataMember(Name = "destinationDataset")]
        public DestinationDataset DestinationDataset { get; set; }
    }
}
