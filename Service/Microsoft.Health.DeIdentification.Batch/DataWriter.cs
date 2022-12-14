using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

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
