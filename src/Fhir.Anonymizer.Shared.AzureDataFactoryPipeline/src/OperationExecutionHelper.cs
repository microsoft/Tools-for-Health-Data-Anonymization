using System;
using System.IO;
using System.Threading.Tasks;

namespace MicrosoftFhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public static class OperationExecutionHelper
    {
        public static readonly Predicate<Exception> IsRetrableException = (ex) =>
        {
            bool result = false;

            if (ex is IOException)
            {
                result = true;
            }

            return result;
        };

        public async static Task<T> InvokeWithTimeoutRetryAsync<T>(Func<Task<T>> func, TimeSpan timeout, int rertyCount, int delayInSec = FhirAzureConstants.StorageOperationRetryDelayInSeconds, Predicate<Exception> isRetrableException = null)
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
                catch (Exception ex)
                {
                    if (isRetrableException?.Invoke(ex) ?? false && rertyCount-- > 0)
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
