// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Batch
{
    public abstract class DataWriter<TSource, TProgress> : BatchProcessor<TSource, TProgress>
    {
        protected override async Task ProcessInternalAsync(Channel<TSource> inputChannel, Channel<TProgress> outputChannel, CancellationToken cancellationToken)
        {
            await base.ProcessInternalAsync(inputChannel, outputChannel, cancellationToken);

            await CommitAsync(cancellationToken);
        }

        protected abstract Task CommitAsync(CancellationToken cancellationToken);
    }
}
