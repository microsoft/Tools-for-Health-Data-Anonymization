using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core.Visitors
{
    public class AnonymizationVisitor : AbstractElementNodeVisitor
    {
        private AnonymizationFhirPathRule[] _rules;
        private Dictionary<string, IAnonymizerProcessor> _processors;
        private HashSet<ElementNode> _visitedNodes = new HashSet<ElementNode>();
        private Stack<ProcessResult> _processResults = new Stack<ProcessResult>();

        public bool AddSecurityTag { get; set; } = true;

        public AnonymizationVisitor(AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _rules = rules;
            _processors = processors;
        }

        public override bool Visit(ElementNode node)
        {
            if (node.IsFhirResource())
            {
                ProcessResult result = AnonymizeResourceNode(node);
                _processResults.Push(result);
            }

            return true;
        }

        public override void EndVisit(ElementNode node)
        {
            if (node.IsFhirResource())
            {
                ProcessResult result = _processResults.Pop();
                if (_processResults.Count() > 0)
                {
                    _processResults.Peek().Update(result);
                }

                if (AddSecurityTag && !node.IsContainedNode())
                {
                    node.AddSecurityTag(result);
                }
            }
        }

        private ProcessResult AnonymizeResourceNode(ElementNode node)
        {
            ProcessResult result = new ProcessResult();
            string typeString = node.InstanceType;
            var resourceSpecificAndGeneralRules = _rules.Where(r => r.ResourceType.Equals(typeString) || string.IsNullOrEmpty(r.ResourceType));

            foreach (var rule in resourceSpecificAndGeneralRules)
            {
                string method = rule.Method.ToUpperInvariant();
                if (!_processors.ContainsKey(method))
                {
                    continue;
                }

                foreach (var matchNode in node.Select(rule.Expression).Cast<ElementNode>())
                {
                    result.Update(ProcessNode(matchNode, _processors[method], _visitedNodes));
                }
            }

            return result;
        }

        public ProcessResult ProcessNode(ElementNode node, IAnonymizerProcessor processor, HashSet<ElementNode> visitedNodes)
        {
            ProcessResult result = new ProcessResult();
            if (visitedNodes.Contains(node))
            {
                return result;
            }
            
            result = processor.Process(node);
            visitedNodes.Add(node);

            foreach (var child in node.Children().Cast<ElementNode>())
            {
                if (child.IsFhirResource())
                {
                    continue;
                }

                result.Update(ProcessNode(child, processor, visitedNodes));
            }

            return result;
        }
    }
}
