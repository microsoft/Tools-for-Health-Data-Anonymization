// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.DeIdentification.Batch.Extensions
{
    public static class StringBatchDataExtensions
    {
        public static JsonBatchData ToJsonBatchData(this StringBatchData stringBatchData)
        {
            return new JsonBatchData(stringBatchData.Resources.Select(str => JObject.Parse(str)).ToList());
        }
    }
}
