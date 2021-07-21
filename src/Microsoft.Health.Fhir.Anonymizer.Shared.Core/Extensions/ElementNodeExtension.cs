using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeExtension
    {
        public static bool IsAgeDecimalNode(this ElementNode node)
        {
            return node != null &&
                   node.Parent.IsAgeNode() &&
                   string.Equals(node.InstanceType, Constants.DecimalTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsReferenceStringNode(this ElementNode node)
        {
            return node != null &&
                   node.Parent.IsReferenceNode() &&
                   string.Equals(node.Name, Constants.ReferenceStringNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static IEnumerable<ElementNode> CastElementNodes(this IEnumerable<ITypedElement> input)
        {
            return input.Select(ToElement).Cast<ElementNode>();
        }

        private static ITypedElement ToElement(ITypedElement node)
        {
            return node is ScopedNode scopedNode 
                ? scopedNode.Current 
                : node;
        }
    }
}
