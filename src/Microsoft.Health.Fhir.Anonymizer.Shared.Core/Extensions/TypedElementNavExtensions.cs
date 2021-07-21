using System.Collections.Generic;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeNavExtensions
    {
        public static IEnumerable<ITypedElement> ResourceDescendantsWithoutSubResource(this ITypedElement node)
        {
            foreach (var child in node.Children())
            {
                // Skip sub resources in bundle entry and contained list
                if (child.IsFhirResource())
                {
                    continue;
                }

                yield return child;

                foreach (var n in child.ResourceDescendantsWithoutSubResource())
                {
                    yield return n;
                }
            }
        }

        public static IEnumerable<ITypedElement> SelfAndDescendantsWithoutSubResource(this IEnumerable<ITypedElement> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var descendant in node.ResourceDescendantsWithoutSubResource())
                {
                    yield return descendant;
                }
            }
        }
    }
}
