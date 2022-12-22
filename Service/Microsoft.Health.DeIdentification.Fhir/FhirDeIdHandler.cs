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
        private readonly int outputChannelLimit = 1000;
        private readonly int maxRunningOperationCount = 5;
        public async Task<string> ExecuteProcess(List<FhirDeIdOperation> operations, List<Object> context)
        {
            Channel<string> source = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannelLimit)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            new Task(() =>
            {
                foreach (var item in context)
                {
                    source.Writer.WriteAsync(item.ToString());
                }
            }).Start();
            var count = 0;
            var tasks = new List<Task>();
            foreach (var operation in operations)
            {
                Channel<string> target = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannelLimit)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });
                while (await source.Reader.WaitToReadAsync())
                {
                    while (tasks.Count > maxRunningOperationCount)
                    {
                        try
                        {
                            await tasks.First();
                            tasks.RemoveAt(0);
                        } catch(Exception ex)
                        {
                            throw ex;
                        }
                    }
                    if (source.Reader.TryRead(out var value))
                    {
                        tasks.Add(InternalProcess(target, operation, value));
                    }
                    count++;
                    if (count == context.Count) { source.Writer.Complete(); }
                }
                count = 0;
                source = target;
            }
            while (tasks.Count > 0)
            {
                try
                    {
                        await tasks.First();
                        tasks.RemoveAt(0);
                    } catch(Exception ex)
                    {
                        throw ex;
                    }
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
        private async Task InternalProcess(Channel<string> target, FhirDeIdOperation operation, string value)
        {
            await target.Writer.WriteAsync(operation.ProcessSingle(value));
        }
    }
}
