using Microsoft.Health.DeIdentification.Batch.Models;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;
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
        }
    }
}
