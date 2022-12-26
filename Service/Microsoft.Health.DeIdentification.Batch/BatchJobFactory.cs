using Microsoft.Health.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Batch
{
    public class BatchJobFactory : IJobFactory
    {
        public IJob Create(JobInfo jobInfo)
        {
            throw new NotImplementedException();
            // Create FhirJob or DicomJob acording to inputdata
        }
    }
}
