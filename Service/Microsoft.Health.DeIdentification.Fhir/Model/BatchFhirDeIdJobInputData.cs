// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Contract;
using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Fhir.Models
{
    [DataContract]
    public class BatchFhirDeIdJobInputData
    {
        [DataMember(Name = "dataSourceType")]
        public DataSourceType DataSourceType { get; set; }

        [DataMember(Name = "dataSourceVersion")]
        public string DataSourceVersion { get; set; }

        [DataMember(Name = "deIdConfiguration")]
        public DeIdConfiguration DeIdConfiguration { get; set; }

        [DataMember(Name = "sourceDataset")]
        public SourceDataset SourceDataset { get; set; }

        [DataMember(Name = "destinationDataset")]
        public DestinationDataset DestinationDataset { get; set; }

    }
}
