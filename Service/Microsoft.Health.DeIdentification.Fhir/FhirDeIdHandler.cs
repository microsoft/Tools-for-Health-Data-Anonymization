using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdHandler
    {
        private readonly int outputChannelLimit = 100;
        public async Task<string> ExecuteProcess(List<FhirDeIdOperation> operations, List<Object> context)
        {
            Channel<string> source = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannelLimit)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            var task = new Task(() =>
            {
                foreach (var item in context)
                {
                    source.Writer.WriteAsync(item.ToString());
                }
            });
            task.RunSynchronously();
            var count = 0;
            foreach (var operation in operations)
            {
                Channel<string> target = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannelLimit)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });
                while (await source.Reader.WaitToReadAsync())
                {
                    if (source.Reader.TryRead(out var value))
                    {
                        await target.Writer.WriteAsync(operation.ProcessSingle(value));
                    }

                    count++;
                    if (count == context.Count) { source.Writer.Complete(); }
                }
                count = 0;
                source = target;
            }
            var result = new StringBuilder();
            while (await source.Reader.WaitToReadAsync())
            {
                if (source.Reader.TryRead(out var value))
                {
                    result.AppendLine(value);
                }
                count++;
                if (count == context.Count) { source.Writer.Complete(); }
            }
            return result.ToString();
        }
    }
}
