using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeOperationExtensions
    {
        private static readonly PocoStructureDefinitionSummaryProvider s_provider = new PocoStructureDefinitionSummaryProvider();

        public static ElementNode AnonymizeElementNode(this ElementNode node, AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, processors);
            node.Accept(visitor);
            node.RemoveNullChildren();

            return node;
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

        public static void AddSecurityTag(this ElementNode node, ProcessResult result)
        {
            if (node == null)
            {
                return;
            }

            if (!result.IsAbstracted && !result.IsPerturbed && !result.IsRedacted)
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
                node.Add(s_provider, newMetaNode);
            }
            else
            {
                node.Replace(s_provider, metaNode, newMetaNode);
            }
        }
    }
}
