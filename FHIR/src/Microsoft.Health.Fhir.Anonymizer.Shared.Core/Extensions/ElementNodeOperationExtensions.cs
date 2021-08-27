using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Visitors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeOperationExtensions
    {
        public static ElementNode Anonymize(this ElementNode node, AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            AnonymizationVisitor visitor = new AnonymizationVisitor(rules, processors);
            node.Accept(visitor);
            node.RemoveEmptyNodes();

            return node;
        }

        public static void RemoveEmptyNodes(this ElementNode node)
        {
            if (node == null)
            {
                return;
            }

            var children = node.Children().ToList();
            foreach (var child in children)
            {
                var elementNodeChild = (ElementNode)child;

                // Remove empty nodes recursively
                RemoveEmptyNodes(elementNodeChild);

                if (IsEmptyNode(elementNodeChild))
                {
                    node.Remove(elementNodeChild);
                }
            }
        }

        private static bool IsEmptyNode(ITypedElement node)
        {
            EnsureArg.IsNotNull(node);

            // A node is considered empty when it has no children and its value is null
            return !node.Children().Any() && node.Value == null && !node.IsFhirResource();
        }
    }
}
