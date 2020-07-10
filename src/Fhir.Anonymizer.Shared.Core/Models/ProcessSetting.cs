using Hl7.Fhir.ElementModel;
using System.Collections.Generic;

namespace Fhir.Anonymizer.Core.Models
{
    public class ProcessSetting
    {
        public string ReplaceWith { get; set; }
        public bool IsPrimitiveReplacement { get; set; }
        public HashSet<ElementNode> VisitedNodes { get; set; }
    }
}
