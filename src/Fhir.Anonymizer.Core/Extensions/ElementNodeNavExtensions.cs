using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeNavExtensions
    {
        public static List<ElementNode> GetEntryResourceChildren(this ElementNode node)
        {
            return node?.Children(Constants.EntryNodeName)
                    .Select(entry => entry?.Children(Constants.ResourceNodeName).FirstOrDefault())
                    .Where(resource => resource != null)
                    .Cast<ElementNode>()
                    .ToList();
        }

        public static List<ElementNode> GetContainedChildren(this ElementNode node)
        {
            return node?.Children(Constants.ContainedNodeName).Cast<ElementNode>().ToList();
        }

        public static IEnumerable<ElementNode> ResourceDescendants(this ElementNode node)
        {
            foreach (var child in node.Children().Cast<ElementNode>())
            {
                // Skip sub resources in bundle entry and contained list
                if (child.IsFhirResource())
                {
                    continue;
                }

                yield return child;

                foreach (var n in child.ResourceDescendants().Cast<ElementNode>())
                {
                    yield return n;
                }
            }
        }

        public static IEnumerable<ElementNode> SelfAndDescendantsWithoutSubResource(this IEnumerable<ElementNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var descendant in node.ResourceDescendants())
                {
                    yield return descendant;
                }
            }
        }
    }
}
