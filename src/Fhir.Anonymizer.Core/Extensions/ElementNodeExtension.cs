using System;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeExtension
    {

        private static readonly string s_locationToFhirPathRegex = @"\[.*?\]";

        public static bool IsDateNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.DateTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsDateTimeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.DateTimeTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsAgeDecimalNode(this ElementNode node)
        {
            return node != null && 
                node.Parent.IsAgeNode() &&
                string.Equals(node.InstanceType, Constants.DecimalTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsInstantNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.InstantTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsAgeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.AgeTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsBundleNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.BundleTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsPostalCodeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, Constants.PostalCodeNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsEntryNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, Constants.EntryNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsContainedNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, Constants.ContainedNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasContainedNode(this ElementNode node)
        {
            return node != null && node.Children(Constants.ContainedNodeName).Any();
        }

        public static bool IsFhirResource(this ElementNode node)
        {
            return node != null && (node.Definition?.IsResource ?? false);
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

        public static ElementNode GetMeta(this ElementNode node)
        {
            return node?.Children("meta").Cast<ElementNode>().FirstOrDefault();
        }
    }
}
