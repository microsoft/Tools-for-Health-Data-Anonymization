using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdBatchProcessor : BatchProcessor<string, string>
    {
        private IDeIdOperation<string, string> _operation;

        public FreeTextDeIdBatchProcessor(IDeIdOperation<string, string> operation)
        {
            _operation = operation;
        }

        public override string[] BatchProcessFunc(BatchInput<string> input)
        {
            return input.Sources.Select(source => _operation.Process(source)).ToArray();
        }
    }
}
