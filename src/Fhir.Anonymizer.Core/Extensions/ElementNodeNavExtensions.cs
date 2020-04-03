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
    }
}
