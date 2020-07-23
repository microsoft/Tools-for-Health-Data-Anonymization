using Hl7.Fhir.ElementModel;
using System.Collections.Generic;

namespace Fhir.Anonymizer.Core.Models
{
    public class ProcessContext
    {
        public HashSet<ElementNode> VisitedNodes { get; set; }
    }
}
