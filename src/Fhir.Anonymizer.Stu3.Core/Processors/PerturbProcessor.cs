using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace Fhir.Anonymizer.Core.Processors
{
    public partial class PerturbProcessor : IAnonymizerProcessor
    {
        private static readonly HashSet<string> s_quantityTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Age.ToString(),
            FHIRAllTypes.Count.ToString(),
            FHIRAllTypes.Duration.ToString(),
            FHIRAllTypes.Distance.ToString(),
            FHIRAllTypes.Money.ToString(),
            FHIRAllTypes.Quantity.ToString(),
            FHIRAllTypes.SimpleQuantity.ToString()
        };
    }
}