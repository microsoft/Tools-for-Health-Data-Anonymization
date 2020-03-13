using System;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public static class ExecutionWithTimeoutRetry
    {
        public async static Task<T> InvokeAsync<T>(Func<Task<T>> func, TimeSpan timeout, int rertyCount)
        {
            while (true)
            {
                try
                {
                    var timeoutTask = Task.Delay(timeout);
                    var executionTask = func();
                    var completedTask = await Task.WhenAny(new Task[] { executionTask, timeoutTask }).ConfigureAwait(false);

                    if (completedTask == executionTask)
                    {
                        return await executionTask.ConfigureAwait(false);
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                }
                catch (TimeoutException)
                {
                    if (rertyCount-- > 0)
                    {
                        continue;
                    }

                    throw;
                }
            }
        } 
    }
}
