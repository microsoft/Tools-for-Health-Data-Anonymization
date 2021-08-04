using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class TypedElementNavExtensions
    {
        public static IEnumerable<ITypedElement> GetEntryResourceChildren(this ITypedElement node)
        {
            return node?.Children(Constants.EntryNodeName)
                .Select(entry => entry?.Children(Constants.EntryResourceNodeName).FirstOrDefault())
                .Where(resource => resource != null);
        }

        public static IEnumerable<ITypedElement> GetContainedChildren(this ITypedElement node)
        {
            return node?.Children(Constants.ContainedNodeName);
        }

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
