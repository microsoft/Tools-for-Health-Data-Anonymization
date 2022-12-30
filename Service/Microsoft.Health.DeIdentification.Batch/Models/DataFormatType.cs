// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Batch.Model
{
    public enum DataFormatType
    {
        [JsonProperty("json")]
        Json,

        [JsonProperty("ndjson")]
        Ndjson,

        [JsonProperty("dicom")]
        dicom,

        [JsonProperty("text")]
        text,
    }
}
