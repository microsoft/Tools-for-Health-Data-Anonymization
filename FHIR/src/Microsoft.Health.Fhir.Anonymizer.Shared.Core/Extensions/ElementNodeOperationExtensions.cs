using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Visitors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeOperationExtensions
    {
        private static readonly PocoStructureDefinitionSummaryProvider s_provider = new PocoStructureDefinitionSummaryProvider();
        private const string _metaNodeName = "meta";

        public static ElementNode Anonymize(this ElementNode node, AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
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

            var children = node.Children().ToList();
            foreach (var child in children)
            {
                var elementNodeChild = (ElementNode)child;

                // Remove null children recursively
                RemoveNullChildren(elementNodeChild);

                if (ShouldRemoveNode(elementNodeChild))
                {
                    node.Remove(elementNodeChild);
                }
            }
        }

        public static void AddSecurityTag(this ElementNode node, ProcessResult result)
        {
            if (node == null || result.ProcessRecords.Count == 0)
            {
                return;
            }

            ElementNode metaNode = (ElementNode)node.GetMeta();
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

            if (result.IsGeneralized && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.GENERALIZED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.GENERALIZED);
            }

            ElementNode newMetaNode = ElementNode.FromElement(meta.ToTypedElement());
            if (metaNode == null)
            {
                node.Add(s_provider, newMetaNode, _metaNodeName);
            }
            else
            {
                node.Replace(s_provider, metaNode, newMetaNode);
            }
        }

        private static bool ShouldRemoveNode(ITypedElement node)
        {
            if (node == null)
            {
                return true;
            }

            return !node.Children().Any() && node.Value == null && !node.IsFhirResource();
        }
    }
}
