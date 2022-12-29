// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Batch.Model
{
    [DataContract]
    public class SourceDataset
    { 
        [DataMember(Name = "format")]
        public DataFormatType DataFormatType { get; set; }

        [DataMember(Name = "dataStoreType")]
        public DataStoreType DataStoreType { get; set; }

        [DataMember(Name = "url")]
        public string URL { get; set; }
    }
}
