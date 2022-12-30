// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.DeIdentification.Batch.Models.Data
{
    /// <summary>
    /// Batch data deserialized to JSON objects.
    /// </summary>
    public class JsonBatchData
    {
        public JsonBatchData(IList<JObject> resources)
        {
            Resources = resources;
        }

        [JsonProperty("resources")]

        public IList<JObject> Resources { get; set; }
    }
}
