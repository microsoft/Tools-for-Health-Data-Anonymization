using Hl7.Fhir.ElementModel;
using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    public class ProcessContext
    {
        public HashSet<ElementNode> VisitedNodes { get; set; }
    }
}
