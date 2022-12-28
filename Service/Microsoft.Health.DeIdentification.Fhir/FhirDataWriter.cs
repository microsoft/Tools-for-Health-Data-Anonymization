using Microsoft.Health.DeIdentification.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDataWriter : DataWriter<ResourceList, ResourceList>
    {
        public override ResourceList[] BatchProcessFunc(BatchInput<ResourceList> input)
        {
            return input.Sources.ToArray();
        }

        protected override Task CommitAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
