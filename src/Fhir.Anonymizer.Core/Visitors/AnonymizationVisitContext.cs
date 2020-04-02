using System;
using System.Collections.Generic;
using System.Text;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public class AnonymizationVisitContext
    {
        public HashSet<ElementNode> VisitedNodes { get; set; } = new HashSet<ElementNode>();


    }
}
