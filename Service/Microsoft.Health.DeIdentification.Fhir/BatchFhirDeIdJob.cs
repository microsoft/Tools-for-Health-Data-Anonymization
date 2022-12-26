using Microsoft.Health.JobManagement;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class BatchFhirDeIdJob : IJob
    {
        public Task<string> ExecuteAsync(JobInfo jobInfo, IProgress<string> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}