using System.Collections.Generic;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    public class ProcessContext
    {
        public HashSet<ITypedElement> VisitedNodes { get; set; }
    }
}
