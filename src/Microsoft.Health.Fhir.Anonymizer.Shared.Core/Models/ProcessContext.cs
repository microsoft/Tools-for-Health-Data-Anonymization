using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    public class ProcessContext
    {
        // The location of visited nodes, e.g., Patient.name[0].use[0]
        public HashSet<string> VisitedNodes { get; set; }
    }
}
