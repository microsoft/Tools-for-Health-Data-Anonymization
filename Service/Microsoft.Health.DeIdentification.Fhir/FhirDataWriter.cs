using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Fhir.Model;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDataWriter : DataWriter<BatchFhirDataContext, OutputInfo>
    {
        public override OutputInfo[] BatchProcessFunc(BatchInput<BatchFhirDataContext> input)
        {
            throw new NotImplementedException();
        }

        protected override Task CommitAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
