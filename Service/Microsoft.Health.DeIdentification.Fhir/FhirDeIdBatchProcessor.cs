using Microsoft.Health.DeIdentification.Batch;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdBatchProcessor : BatchProcessor<string, string>
    {
        public override string[] BatchProcessFunc(BatchInput<string> input)
        {
            throw new NotImplementedException();
        }
    }
}