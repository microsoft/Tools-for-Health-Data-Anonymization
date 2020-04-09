using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeNavExtensions
    {
        internal static readonly string s_containedNodeName = "contained";
        internal static readonly string s_entryNodeName = "entry";
        internal static readonly string s_resourceNodeName = "resource";

        public static List<ElementNode> GetEntryResourceChildren(this ElementNode node)
        {
            return node?.Children(s_entryNodeName)
                    .Select(entry => entry?.Children(s_resourceNodeName).FirstOrDefault())
                    .Where(resource => resource != null)
                    .Cast<ElementNode>()
                    .ToList();
        }

        public static List<ElementNode> GetContainedChildren(this ElementNode node)
        {
            return node?.Children(s_containedNodeName).Cast<ElementNode>().ToList();
        }

        public static IEnumerable<ElementNode> SubResourceNodesAndSelf(this ElementNode node)
        {
            yield return node;

            foreach (var child in node.Descendants().Cast<ElementNode>())
            {
                if (child.IsBundleNode())
                {
                    foreach (var bundleNode in child.GetEntryResourceChildren())
                    {
                        yield return bundleNode;
                    }
                }
                else if (child.IsContainedNode())
                {
                    yield return child;
                }
            }
        }

        public static IEnumerable<ElementNode> ResourceDescendants(this ElementNode node)
        {
            foreach (var child in node.Children().Cast<ElementNode>())
            {
                // Skip sub resources in bundle entry and contained list
                if (child.IsEntryNode() || child.IsContainedNode())
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

        public static IEnumerable<ElementNode> ResourceDescendantsAndSelf(this IEnumerable<ElementNode> nodes)
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
