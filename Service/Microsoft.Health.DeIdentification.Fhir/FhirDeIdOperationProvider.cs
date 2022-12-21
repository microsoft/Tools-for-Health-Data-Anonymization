using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using System.Text;
using System.Threading.Channels;
using Xunit.Sdk;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperationProvider : IDeIdOperationProvider
    {
        private readonly int outputChannedlLimit = 100;
        private readonly string pathPrefix = "../Microsoft.Health.DeIdentification.Local/configurations/";
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdConfiguration deIdConfiguration)
        {
            throw new NotImplementedException();
        }

        public List<FhirDeIdOperation> CreateDeIdOperations(DeIdConfiguration deIdConfiguration)
        {
            List<FhirDeIdOperation> operations = new List<FhirDeIdOperation>();
            foreach (var item in deIdConfiguration.ModelConfigReferences)
            {
                operations.Add(new FhirDeIdOperation($"{pathPrefix+item.Value}"));
            }
            return operations;
        }

        public async Task<string> ExecuteProcess(List<FhirDeIdOperation> operations, List<Object> context)
        {
            Channel<string> source = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannedlLimit)
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
                Channel<string> target = Channel.CreateBounded<string>(new BoundedChannelOptions(outputChannedlLimit)
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