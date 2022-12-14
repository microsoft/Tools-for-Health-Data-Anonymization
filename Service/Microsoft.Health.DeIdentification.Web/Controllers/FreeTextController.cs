using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FreeTextController
    {
        // Post: 
        public void DeIdentification()
        {

        }

        // Post: start batch job
        public void BatchDeIdentification()
        {
            // Create Job
            // Return id to customer
        }

        // DELETE: Cancel batch job
        public void CancelDeIdentificationJob(string jobid)
        {
            // Cancel Job
            // Return status
        }

        // GET: Get job progress
        public void GetDeIdentificationJobStatus(string jobid)
        {
            // Get Job
            // Return job with progress
        }
    }
}
