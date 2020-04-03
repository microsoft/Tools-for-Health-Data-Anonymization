using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeExtension
    {
        // InstanceType constants
        internal static readonly string s_dateTypeName = "date";
        internal static readonly string s_dateTimeTypeName = "dateTime";
        internal static readonly string s_decimalTypeName = "decimal";
        internal static readonly string s_instantTypeName = "instant";
        internal static readonly string s_ageTypeName = "Age";
        internal static readonly string s_bundleTypeName = "Bundle";

        // NodeName constants
        internal static readonly string s_postalCodeNodeName = "postalCode";
        internal static readonly string s_containedNodeName = "contained";
        internal static readonly string s_entryNodeName = "entry";
        internal static readonly string s_resourceNodeName = "resource";

        private static readonly string s_locationToFhirPathRegex = @"\[.*?\]";

        public static bool IsDateNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, s_dateTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsDateTimeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, s_dateTimeTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsAgeDecimalNode(this ElementNode node)
        {
            return node != null && 
                node.Parent.IsAgeNode() &&
                string.Equals(node.InstanceType, s_decimalTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsInstantNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, s_instantTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsAgeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, s_ageTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsBundleNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, s_bundleTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsPostalCodeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, s_postalCodeNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsEntryNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, s_entryNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsContainedNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, s_containedNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasContainedNode(this ElementNode node)
        {
            return node != null && node.Children(s_containedNodeName).Any();
        }

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

        public static string GetFhirPath(this ElementNode node)
        {
            return node == null ? string.Empty : Regex.Replace(node.Location, s_locationToFhirPathRegex, string.Empty);
        }

        public static string GetNodeId(this ElementNode node)
        {
            var id = node.Children("id").FirstOrDefault();
            return id?.Value?.ToString() ?? string.Empty;
        }
    }
}
