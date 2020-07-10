using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath.Sprache;

namespace Fhir.Anonymizer.Core.Processors
{
    public class SubstituteProcessor : IAnonymizerProcessor
    {
        private readonly FhirJsonParser _parser = new FhirJsonParser();

        public ProcessResult Process(ElementNode node, ProcessSetting setting = null)
        {
            if (setting == null)
            {
                setting = new ProcessSetting
                {
                    ReplaceWith = string.Empty,
                    IsPrimitiveReplacement = true,
                    VisitedNodes = new HashSet<ElementNode>()
                };
            }
            else if (setting.ReplaceWith == null)
            {
                setting.ReplaceWith = string.Empty;
            }
            else if (setting.VisitedNodes == null)
            {
                setting.VisitedNodes = new HashSet<ElementNode>();
            }

            ElementNode replacementNode;
            // Get replacementNode for substitution  
            if (setting.IsPrimitiveReplacement)
            {
                replacementNode = ElementNode.FromElement(ElementNode.ForPrimitive(setting.ReplaceWith));
            }
            else
            {
                var modelAssembly = typeof(Patient).Assembly;
                var replacementNodeType = modelAssembly
                    .GetTypes()
                    .Where(type => string.Equals(type.Name, node.InstanceType, StringComparison.InvariantCultureIgnoreCase))
                    .First();

                var replaceElement = _parser.Parse(setting.ReplaceWith, replacementNodeType).ToTypedElement();
                replacementNode = ElementNode.FromElement(replaceElement);
            }

            var keepNodes = new HashSet<ElementNode>();
            // Retrieve all nodes that have been processed before to keep 
            _ = SubstituteUtility.ShouldKeepNodeDuringSubstitution(node, setting.VisitedNodes, keepNodes);
            var processResult = SubstituteUtility.SubstituteNode(node, replacementNode, setting.VisitedNodes, keepNodes);
            SubstituteUtility.MarkSubstitutedFragementAsVisited(node, setting.VisitedNodes);

            return processResult;
        }
    }
}
