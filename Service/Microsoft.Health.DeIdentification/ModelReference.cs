// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Contract
{
    [DataContract]
    public class ModelReference
    {
        [DataMember(Name = "modelType")]
        public DeidModelType ModelType { get; set; }

        [DataMember(Name = "configurationLocation")]
        public string ConfigurationLocation { get; set; }

    }
}
