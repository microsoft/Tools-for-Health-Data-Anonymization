// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DeIdentification.Batch
{
    public class BatchInput<TSource>
    {
        public TSource[] Sources { get; set; }

        public int StartIndex { get; set; }

        public string[] Select()
        {
            throw new NotImplementedException();
        }
    }
}
