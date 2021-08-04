using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class TypedElementExtensions
    {
        private static readonly string s_locationToFhirPathRegex = @"\[.*?\]";

        public static bool IsDateNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.DateTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsDateTimeNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.DateTimeTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsInstantNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.InstantTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsAgeNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.AgeTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsBundleNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.BundleTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsReferenceNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.ReferenceTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsPostalCodeNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.Name, Constants.PostalCodeNodeName, StringComparison.InvariantCultureIgnoreCase);
        }


        public static bool IsEntryNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.Name, Constants.EntryNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsContainedNode(this ITypedElement node)
        {
            return node != null && string.Equals(node.Name, Constants.ContainedNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasContainedNode(this ITypedElement node)
        {
            return node != null && node.Children(Constants.ContainedNodeName).Any();
        }

        public static bool IsFhirResource(this ITypedElement node)
        {
            return node != null && (node.Definition?.IsResource ?? false);
        }

        public static string GetFhirPath(this ITypedElement node)
        {
            return node == null ? string.Empty : Regex.Replace(node.Location, s_locationToFhirPathRegex, string.Empty);
        }

        public static string GetNodeId(this ITypedElement node)
        {
            var id = node.Children("id").FirstOrDefault();
            return id?.Value?.ToString() ?? string.Empty;
        }

        public static ITypedElement GetMeta(this ITypedElement node)
        {
            return node?.Children("meta").FirstOrDefault();
        }

        public static IEnumerable<ElementNode> CastElementNodes(this IEnumerable<ITypedElement> input)
        {
            return input.Select(ToElement).Cast<ElementNode>();
        }

        public static ITypedElement ToElement(this ITypedElement node)
        {
            return node is ScopedNode scopedNode
                ? scopedNode.Current
                : node;
        }
    }
}
