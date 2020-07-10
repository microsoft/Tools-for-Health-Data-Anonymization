using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;

namespace Fhir.Anonymizer.Core.Utility
{
    public class SubstituteUtility
    {
        private static readonly PocoStructureDefinitionSummaryProvider s_provider = new PocoStructureDefinitionSummaryProvider();

        public static ProcessResult SubstituteNode(ElementNode node, ElementNode replacementNode, HashSet<ElementNode> visitedNodes, HashSet<ElementNode> keepNodes)
        {
            var processResult = new ProcessResult();
            if (node == null || replacementNode == null || visitedNodes.Contains(node))
            {
                return processResult;
            }

            // children names to replace, multiple to multiple replacement
            var replaceChildrenNames = replacementNode.Children().Select(element => element.Name).ToHashSet();
            foreach (var name in replaceChildrenNames)
            {
                var children = node.Children(name).Cast<ElementNode>().ToList();
                var targetChildren = replacementNode.Children(name).Cast<ElementNode>().ToList();

                int i = 0;
                foreach (var child in children)
                {
                    if (visitedNodes.Contains(child))
                    {
                        // Skip replacement if child already processed before.
                        i++;
                        continue;
                    }
                    else if (i < targetChildren.Count)
                    {
                        // We still have target nodes, do replacement
                        processResult.Update(SubstituteNode(child, targetChildren[i ++], visitedNodes, keepNodes));
                    }
                    else if (keepNodes.Contains(child))
                    {
                        // Substitute with an empty node when no target node available but we need to keep this node
                        processResult.Update(SubstituteNode(child, GetDummyNode(), visitedNodes, keepNodes));
                    }
                    else
                    {
                        // Remove source node when no target node availabe and we don't need to keep the source node
                        node.Remove(child);
                        processResult.AddProcessRecord(AnonymizationOperations.Substitute, child);
                    }
                }

                while (i < targetChildren.Count)
                {
                    // Add extra target nodes, create a new copy before adding
                    node.Add(s_provider, ElementNode.FromElement(targetChildren[i ++]));
                }
            }

            // children nodes not presented in replacement value, we need either remove or keep a dummy copy
            var nonReplacementChildren = node.Children()
                .Where(element => !replaceChildrenNames.Contains(element.Name))
                .Cast<ElementNode>().ToList();
            foreach (var child in nonReplacementChildren)
            {
                if (visitedNodes.Contains(child))
                {
                    continue;
                }
                else if (keepNodes.Contains(child))
                {
                    SubstituteNode(child, GetDummyNode(), visitedNodes, keepNodes);
                }
                else
                {
                    node.Remove(child);
                }
            }

            node.Value = replacementNode.Value;
            processResult.AddProcessRecord(AnonymizationOperations.Substitute, node);
            return processResult;
        }

        // To keep consistent anonymization changes made by preceding rules, we should figure out whether a node can be removed during substitution
        public static bool ShouldKeepNodeDuringSubstitution(ElementNode node, HashSet<ElementNode> visitedNodes, HashSet<ElementNode> keepNodes)
        {
            var shouldKeep = false;
            // If a child (no matter how deep) has been modified, this node should be kept
            foreach (var child in node.Children().Cast<ElementNode>())
            {
                shouldKeep |= ShouldKeepNodeDuringSubstitution(child, visitedNodes, keepNodes);
            }

            // If this node its self has been modified, it should be kept
            if (shouldKeep || visitedNodes.Contains(node))
            {
                keepNodes.Add(node);
                return true;
            }

            return shouldKeep;
        }

        // Post-process to mark all substituted children nodes as visited
        public static void MarkSubstitutedFragementAsVisited(ElementNode node, HashSet<ElementNode> visitedNodes)
        {
            visitedNodes.Add(node);
            foreach (var child in node.Children().Cast<ElementNode>())
            {
                MarkSubstitutedFragementAsVisited(child, visitedNodes);
            }
        }

        private static ElementNode GetDummyNode()
        {
            var dummy = ElementNode.FromElement(ElementNode.ForPrimitive(string.Empty));
            // Set dummy value to null to ensure a correct serialization result
            dummy.Value = null;
            return dummy;
        }
    }
}
