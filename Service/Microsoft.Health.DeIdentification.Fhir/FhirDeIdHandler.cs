using System.Collections;
using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdHandler
    {
        private readonly int outputChannelLimit = 1000;
        private readonly int maxRunningOperationCount = 5;
        private readonly int maxContextCount = 700;
        public async Task<ResourceList> ExecuteProcess(List<FhirDeIdOperation> operations, IList context)
        {
            if ( context.Count >= maxContextCount)
            {
                throw new Exception("Context count can't be greater than 700");
            }
            try
            {
                Channel<string> source = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannelLimit)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });
                var enqueueTask = Task.Run(() =>
                {
                    foreach (var item in context)
                    {
                        source.Writer.WriteAsync(item.ToString());
                    }
                    source.Writer.Complete();
                });
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
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                        if (source.Reader.TryRead(out var value))
                        {
                            tasks.Add(InternalProcess(target, operation, value));
                        }
                    }
                    source = target;
                    source.Writer.Complete();
                }
                while (tasks.Count > 0)
                {
                    try
                    {
                        await tasks.First();
                        tasks.RemoveAt(0);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                var result = new List<object>();
                while (await source.Reader.WaitToReadAsync())
                {
                    if (source.Reader.TryRead(out var value))
                    {
                        result.Add(value);
                    }
                    else
                    {
                        source.Writer.Complete();
                    }
                }
                return new ResourceList() { Resources = result };
            } catch(Exception ex)
            {
                throw ex;
            }
            
        }
        private async Task InternalProcess(Channel<string> target, FhirDeIdOperation operation, string value)
        {
            await target.Writer.WriteAsync(operation.ProcessSingle(value));
        }
    }
}
