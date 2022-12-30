// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch.Models.Data;

namespace Microsoft.Health.DeIdentification.Batch.Extensions
{
    public static class JsonBatchDataExtensions
    {
        public static StringBatchData ToStringBatchData(this JsonBatchData jsonBatchData)
        {
            return new StringBatchData(jsonBatchData.Resources.Select(jobject => jobject.ToString()).ToList());
        }
    }
}
