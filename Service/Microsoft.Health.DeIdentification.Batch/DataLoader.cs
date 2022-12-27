// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Batch
{
    public abstract class DataLoader<TSource>
    {
        public int OutputChannelLimit { get; set; } = 100;

        public (Channel<TSource> outputChannel, Task loadTask) Load(CancellationToken cancellationToken)
        {
            Channel<TSource> outputChannel = Channel.CreateBounded<TSource>(OutputChannelLimit);

            Task loadTask = Task.Run(
                async () =>
                {
                    await LoadDataInternalAsync(outputChannel, cancellationToken);
                },
                cancellationToken);

            return (outputChannel, loadTask);
        }

        protected abstract Task LoadDataInternalAsync(Channel<TSource> outputChannel, CancellationToken cancellationToken);
    }
}
