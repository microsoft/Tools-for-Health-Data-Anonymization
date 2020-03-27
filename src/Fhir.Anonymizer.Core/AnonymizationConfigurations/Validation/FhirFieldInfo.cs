using System.Collections.Generic;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation
{
    public class FhirFieldInfo
    {
        public string FieldName { get; set; }
        // A field can be more than one type on Choice Elements
        public IEnumerable<string> TypeNames { get; set; } 
    }
}
