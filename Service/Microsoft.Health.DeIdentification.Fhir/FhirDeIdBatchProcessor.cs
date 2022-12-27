using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdBatchProcessor : BatchProcessor<string, string>
    {
        private IDeIdOperation<string, string> _operation;

        public FhirDeIdBatchProcessor(IDeIdOperation<string, string> operation)
        {
            _operation = operation;
        }

        public override string[] BatchProcessFunc(BatchInput<string> input)
        {
            return input.Sources.Select(source => _operation.Process(source)).ToArray();
        }
    }
}