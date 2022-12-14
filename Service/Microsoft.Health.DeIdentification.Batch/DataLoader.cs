using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

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
