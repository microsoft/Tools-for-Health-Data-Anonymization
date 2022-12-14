using Microsoft.Health.DeIdentification.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Azure
{
    public class AzureBlobTextWriter : DataWriter<string, string>
    {
        public override string[] BatchProcessFunc(BatchInput<string> input)
        {
            // Write block to blob and return new string[] { "Progress Message" }
            throw new NotImplementedException();
        }

        protected override Task CommitAsync(CancellationToken cancellationToken)
        {
            // Commit all block id
            throw new NotImplementedException();
        }
    }
}
