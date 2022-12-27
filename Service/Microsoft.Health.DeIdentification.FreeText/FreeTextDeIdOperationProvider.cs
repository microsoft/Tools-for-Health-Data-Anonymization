// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdOperationProvider : IDeIdOperationProvider
    {
        public IList<IDeIdOperation<TSource, TResult>> CreateDeIdOperations<TSource, TResult>(DeIdConfiguration deIdConfiguration)
        {
            // return null for format mismatch
            throw new NotImplementedException();
        }
    }
}
