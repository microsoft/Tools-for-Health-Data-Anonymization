// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Batch.Model
{
    public enum DataStoreType
    {
        [JsonProperty("local")]
        Local,

        [JsonProperty("azureBlob")]
        AzureBlob,
    }
}
