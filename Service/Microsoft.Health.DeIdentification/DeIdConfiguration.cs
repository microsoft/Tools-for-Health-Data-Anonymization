﻿using System.Runtime.Serialization;

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

        public Dictionary<DeidModelType, string> ModelConfigReferences { get; set; }

        [DataMember(Name = "settings")]
        public Dictionary<string, string> Settings { get; set; }
    }
}
