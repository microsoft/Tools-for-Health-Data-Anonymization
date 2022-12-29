// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class ResourceList
    {
        [JsonProperty("resources")]
        public IList Resources { get; set; }

        public string inputFileName { get; set; }

        public string outputFileName { get; set; }
    }
}
