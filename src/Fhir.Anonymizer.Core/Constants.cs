using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Core
{
    internal static class Constants
    {
        internal const int DefaultPartitionedExecutionCount = 5;
        internal const int DefaultPartitionedExecutionBatchSize = 1000;
    }
}
