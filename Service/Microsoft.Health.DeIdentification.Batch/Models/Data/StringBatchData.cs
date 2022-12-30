// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.DeIdentification.Batch.Models.Data
{
    /// <summary>
    /// Batch data deserialized to string.
    /// </summary>
    public class StringBatchData
    {
        public StringBatchData()
        {
            Resources = new List<string>() ;
        }

        public StringBatchData(IList<string> resources)
        {
            Resources = resources;
        }

        public StringBatchData(IList<JObject> jobjects)
        {
            Resources = jobjects.Select(jobject => jobject.ToString()).ToList();
        }

        [JsonProperty("resources")]
        public IList<string> Resources { get; set; }
    }
}
