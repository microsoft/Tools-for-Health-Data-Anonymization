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
        private Stack<Tuple<ElementNode, ProcessResult>> _contextStack = new Stack<Tuple<ElementNode, ProcessResult>>();

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
                ProcessResult result = ProcessResourceNode(node);
                _contextStack.Push(new Tuple<ElementNode, ProcessResult>(node, result));
            }

            return true;
        }

        public override void EndVisit(ElementNode node)
        {
            if (node.IsFhirResource())
            {
                Tuple<ElementNode, ProcessResult> context = _contextStack.Pop();
                ProcessResult result = context.Item2;

                if (context.Item1 != node)
                {
                    // Should never throw exception here. In case any bug happen, we can get clear message for this exception.
                    throw new ConstraintException("Internal error: access wrong context.");
                }
                
                if (_contextStack.Count() > 0)
                {
                    _contextStack.Peek().Item2.Update(result);
                }

                if (AddSecurityTag && !node.IsContainedNode())
                {
                    node.AddSecurityTag(result);
                }
            }
        }

        private ProcessResult ProcessResourceNode(ElementNode node)
        {
            ProcessResult result = new ProcessResult();
            string typeString = node.InstanceType;
            IEnumerable<AnonymizationFhirPathRule> resourceSpecificAndGeneralRules = GetRulesByType(typeString);

            foreach (var rule in resourceSpecificAndGeneralRules)
            {
                string method = rule.Method.ToUpperInvariant();
                if (!_processors.ContainsKey(method))
                {
                    continue;
                }

                foreach (var matchNode in node.Select(rule.Expression).Cast<ElementNode>())
                {
                    result.Update(ProcessNodeRecursive(matchNode, _processors[method], _visitedNodes));
                }
            }

            return result;
        }

        private IEnumerable<AnonymizationFhirPathRule> GetRulesByType(string typeString)
        {
            return _rules.Where(r => r.ResourceType.Equals(typeString) 
                                    || string.IsNullOrEmpty(r.ResourceType) 
                                    || string.Equals(Constants.GeneralResourceType, r.ResourceType)
                                    || string.Equals(Constants.GeneralDomainResourceType, r.ResourceType));
        }

        public ProcessResult ProcessNodeRecursive(ElementNode node, IAnonymizerProcessor processor, HashSet<ElementNode> visitedNodes)
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

                result.Update(ProcessNodeRecursive(child, processor, visitedNodes));
            }

            return result;
        }
    }
}
