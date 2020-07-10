using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath.Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fhir.Anonymizer.Core.Processors
{
    public class SubstituteProcessor : IAnonymizerProcessor
    {
        private readonly FhirJsonParser _parser = new FhirJsonParser();

        public ProcessResult Process(ElementNode node, ProcessSetting setting = null)
        {
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
            _ = SubstituteUtility.ShouldKeepNodeDuringSubstitution(node, setting.VisitedNodes, keepNodes);

            var processResult = SubstituteUtility.SubstituteNode(node, replacementNode, setting.VisitedNodes, keepNodes);
            SubstituteUtility.MarkSubstitutedChildrenAsVisited(node, setting.VisitedNodes);

            return processResult;
        }
    }
}
