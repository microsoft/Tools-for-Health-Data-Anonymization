// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Contract
{
    [DataContract]
    public class DeIdConfiguration
    {
        /// <summary>
        /// the name of deid configuration
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "dataSourceType")]
        public DataSourceType DataSourceType { get; set; }

        [DataMember(Name = "dataSourceVersion")]
        public string DataSourceVersion { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "modelConfigReferences")]

        public List<ModelReference> ModelConfigReferences { get; set; }

        [DataMember(Name = "settings")]
        public Dictionary<string, string> Settings { get; set; }
    }
}
