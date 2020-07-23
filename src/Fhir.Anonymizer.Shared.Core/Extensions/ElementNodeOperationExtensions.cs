using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
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

        public static ElementNode Anonymize(this ElementNode node, AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, processors);
            node.Accept(visitor);
            node.RemoveNullChildren();

            return node;
        }

        // Remove null children of current node, and return true => current node is null
        public static bool RemoveNullChildren(this ElementNode node)
        {
            if (node == null)
            {
                return true;
            }

            var children = node.Children().Cast<ElementNode>().ToList();
            foreach (var child in children)
            {
                // Remove child if it is null => return true
                if (RemoveNullChildren(child))
                {
                    node.Remove(child);
                }
            }

            bool currentNodeIsEmpty = !node.Children().Any() && node.Value == null;
            bool currentNodeIsFhirResource = node.IsFhirResource();
            if (currentNodeIsEmpty && !currentNodeIsFhirResource)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void AddSecurityTag(this ElementNode node, ProcessResult result)
        {
            if (node == null)
            {
                return;
            }

            if (result.ProcessRecords.Count == 0)
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

            if (result.IsCryptoHashed && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.CRYTOHASH.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.CRYTOHASH);
            }

            if (result.IsEncrypted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.ENCRYPT.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.ENCRYPT);
            }

            if (result.IsPerturbed && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.PERTURBED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.PERTURBED);
            }

            if (result.IsSubstituted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.SUBSTITUTED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.SUBSTITUTED);
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
