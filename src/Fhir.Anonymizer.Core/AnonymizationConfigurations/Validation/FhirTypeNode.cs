using System;
using System.Collections.Generic;
using System.Text;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation
{
    public class FhirTypeNode
    {
        public bool IsResource { get; set; }
        public bool IsNested { get; set; }
        public string InstanceType { get; set; }
        public string Name { get; set; }
        public Dictionary<string, IEnumerable<FhirTypeNode>> Children { get; set; }
        public FhirTypeNode Parent { get; set; }
    }
}
