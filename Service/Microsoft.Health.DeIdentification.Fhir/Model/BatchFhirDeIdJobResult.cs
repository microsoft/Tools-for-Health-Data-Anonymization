// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Fhir.Models
{
    [DataContract]
    public class BatchFhirDeIdJobResult
    {
        [DataMember(Name = "outputs")]
        public List<OutputInfo> Outputs { get; set; } = new List<OutputInfo>();
    }
}
