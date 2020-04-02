using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeNavExtensions
    {
        public static IEnumerable<ElementNode> SubResourceNodesAndSelf(this ElementNode node)
        {
            yield return node;
        }
    }
}
