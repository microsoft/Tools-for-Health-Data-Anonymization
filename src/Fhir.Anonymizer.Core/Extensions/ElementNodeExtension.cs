using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;

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
        internal static readonly string s_entryResourceNodeName = "resource";

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

        public static bool IsEntryResourceNode(this ElementNode node)
        {
            return node != null
                && string.Equals(node.Name, s_entryResourceNodeName, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(node.Parent?.Name, s_entryNodeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasContainedNode(this ElementNode node)
        {
            return node != null && node.Children(s_containedNodeName).Any();
        }

        public static bool IsFhirResource(this ElementNode node)
        {
            return node.Definition?.IsResource ?? false;
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
            return node.Children("meta").Cast<ElementNode>().FirstOrDefault();
        }

        public static void RemoveNullChildren(this ElementNode node)
        {
            if (node == null)
            {
                return;
            }

            var children = node.Children().Cast<ElementNode>().ToList();
            foreach (var child in children)
            {
                RemoveNullChildren(child);
            }

            if (!node.Children().Any() && node.Value == null && !Enum.TryParse<ResourceType>(node.InstanceType, true, out _))
            {
                node.Parent.Remove(node);
                return;
            }
        }

        public static void AddSecurityTag(this ElementNode node, ProcessResult result, IStructureDefinitionSummaryProvider provider)
        {
            if (node == null)
            {
                return;
            }

            ElementNode metaNode = node.GetMeta();
            Meta meta = metaNode?.ToPoco<Meta>() ?? new Meta();

            if (result.IsRedacted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.REDACT.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.REDACT);
            }

            if (result.IsAbstracted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.ABSTRED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.ABSTRED);
            }

            if (result.IsPerturbed && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.PERTURBED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.PERTURBED);
            }

            ElementNode newMetaNode = ElementNode.FromElement(meta.ToTypedElement());
            if (metaNode == null)
            {
                node.Add(provider, newMetaNode);
            }
            else 
            {
                node.Replace(provider, metaNode, newMetaNode);
            }
        }
    }
}
