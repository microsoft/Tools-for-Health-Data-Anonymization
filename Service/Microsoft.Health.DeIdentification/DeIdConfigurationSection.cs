// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Contract
{
    [DataContract]
    public class DeIdConfigurationSection
    {
        [DataMember(Name = "deIdConfigurations")]
        public DeIdConfiguration[] DeIdConfigurations { get; set; }
    }
}
