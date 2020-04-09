using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Core
{
    internal static class Constants
    {
        internal const string PathKey = "path";
        internal const string MethodKey = "method";

        internal const int DefaultPartitionedExecutionCount = 4;
        internal const int DefaultPartitionedExecutionBatchSize = 1000;
    }
}
