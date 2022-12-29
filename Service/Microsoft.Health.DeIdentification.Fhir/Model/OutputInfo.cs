// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Fhir.Models
{
    [DataContract]
    public class OutputInfo
    {
        [DataMember(Name = "sourceUrl")]

        public string SourceUrl { get; set; }

        [DataMember(Name = "outputUrl")]
        public string OutputUrl { get; set; }
    }
}
