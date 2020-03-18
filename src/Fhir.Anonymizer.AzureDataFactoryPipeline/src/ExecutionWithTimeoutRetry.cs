using System;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public static class ExecutionWithTimeoutRetry
    {
        public async static Task<T> InvokeAsync<T>(Func<Task<T>> func, TimeSpan timeout, int rertyCount, int delayInSec = FhirAzureConstants.StorageOperationRetryDelayInSeconds)
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
                        await Task.Delay(TimeSpan.FromSeconds(delayInSec)).ConfigureAwait(false);
                        continue;
                    }

                    throw;
                }
            }
        } 
    }
}
