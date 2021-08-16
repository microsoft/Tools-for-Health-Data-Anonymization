using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Visitors
{
    public class AnonymizationVisitor : AbstractElementNodeVisitor
    {
        private readonly AnonymizationFhirPathRule[] _rules;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;
        private HashSet<ElementNode> _visitedNodes = new HashSet<ElementNode>();
        private Stack<Tuple<ElementNode, ProcessResult>> _contextStack = new Stack<Tuple<ElementNode, ProcessResult>>();
        private Dictionary<string, List<ITypedElement>> _typeToNodeCache = new Dictionary<string, List<ITypedElement>>();
        private Dictionary<string, List<ITypedElement>> _nameToNodeCache = new Dictionary<string, List<ITypedElement>>();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizationVisitor>();

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
                
                if (_contextStack.Any())
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
            // Initialize cache for every resource node
            InitializeNodeCache(node);

            ProcessResult result = new ProcessResult();
            string typeString = node.InstanceType;
            IEnumerable<AnonymizationFhirPathRule> resourceSpecificAndGeneralRules = GetRulesByType(typeString);

            foreach (var rule in resourceSpecificAndGeneralRules)
            {
                ProcessContext context = new ProcessContext
                {
                    VisitedNodes = _visitedNodes
                };

                ProcessResult resultOnRule = new ProcessResult();
                string method = rule.Method.ToUpperInvariant();
                if (!_processors.ContainsKey(method))
                {
                    continue;
                }

                var matchNodes = GetMatchNodes(rule, node);
                foreach (var matchNode in matchNodes)
                {
                    resultOnRule.Update(ProcessNodeRecursive((ElementNode)matchNode.ToElement(), _processors[method], context, rule.RuleSettings));
                }

                LogProcessResult(node, rule, resultOnRule);

                result.Update(resultOnRule);
            }

            return result;
        }

        private void InitializeNodeCache(ITypedElement node)
        {
            _typeToNodeCache.Clear();
            _nameToNodeCache.Clear();

            InitializeNodeCacheRecursive(node);
        }

        private void InitializeNodeCacheRecursive(ITypedElement node)
        {
            foreach (var child in node.Children())
            {
                // Cache instance type
                if (_typeToNodeCache.ContainsKey(child.InstanceType))
                {
                    _typeToNodeCache[child.InstanceType].Add(child);
                }
                else
                {
                    _typeToNodeCache[child.InstanceType] = new List<ITypedElement> { child };
                }

                // Cache name
                if (_nameToNodeCache.ContainsKey(child.Name))
                {
                    _nameToNodeCache[child.Name].Add(child);
                }
                else
                {
                    _nameToNodeCache[child.Name] = new List<ITypedElement> { child };
                }

                // Recursively process node's children except resource children, which will be processed independently as a resource
                if (!child.IsFhirResource())
                {
                    InitializeNodeCacheRecursive(child);
                }
            }
        }

        private IEnumerable<ITypedElement> GetMatchNodes(AnonymizationFhirPathRule rule, ITypedElement node)
        {
            var typeMatch = AnonymizationFhirPathRule.TypeRuleRegex.Match(rule.Path);
            var nameMatch = AnonymizationFhirPathRule.NameRuleRegex.Match(rule.Path);

            if (typeMatch.Success)
            {
                return GetMatchNodesFromLookUp(_typeToNodeCache, typeMatch.Groups["type"].Value, typeMatch.Groups["expression"].Value);
            }
            
            if (nameMatch.Success)
            {
                return GetMatchNodesFromLookUp(_nameToNodeCache, nameMatch.Groups["name"].Value, nameMatch.Groups["expression"].Value);
            }

            /*
            * Special case handling:
            * Senario: FHIR path only contains resourceType: Patient, Resource. 
            * Sample AnonymizationFhirPathRule: { "path": "Patient", "method": "keep" }
            * 
            * Current FHIR path lib do not support navigate such ResourceType FHIR path from resource in bundle.
            * Example: navigate with FHIR path "Patient" from "Bundle.entry[0].resource[0]" is not support
            */
            return rule.IsResourceTypeRule ? new List<ITypedElement> { node } : node.Select(rule.Expression).ToList();
        }

        private static IEnumerable<ITypedElement> GetMatchNodesFromLookUp(Dictionary<string, List<ITypedElement>> lookUp, string key, string expression)
        {
            var matchNodes = new List<ITypedElement>();

            if (!lookUp.ContainsKey(key))
            {
                return matchNodes;
            }

            if (!string.IsNullOrEmpty(expression))
            {
                var nodes = lookUp[key];
                foreach (var node in nodes)
                {
                    matchNodes.AddRange(node.Select(expression));
                }
            }
            else
            {
                matchNodes = lookUp[key];
            }

            return matchNodes;
        }

        private void LogProcessResult(ElementNode node, AnonymizationFhirPathRule rule, ProcessResult resultOnRule)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                string resourceId = node.GetNodeId();
                foreach (var processRecord in resultOnRule.ProcessRecords)
                {
                    foreach (var matchNode in processRecord.Value)
                    {
                        _logger.LogDebug($"[{resourceId}]: Rule '{rule.Path}' matches '{matchNode.Location}' and perform operation '{processRecord.Key}'");
                    }
                }
            }
        }

        private IEnumerable<AnonymizationFhirPathRule> GetRulesByType(string typeString)
        {
            return _rules.Where(r => r.ResourceType.Equals(typeString) 
                                    || string.IsNullOrEmpty(r.ResourceType) 
                                    || string.Equals(Constants.GeneralResourceType, r.ResourceType)
                                    || string.Equals(Constants.GeneralDomainResourceType, r.ResourceType));
        }

        public ProcessResult ProcessNodeRecursive(ElementNode node, IAnonymizerProcessor processor, ProcessContext context, Dictionary<string, object> settings)
        {
            ProcessResult result = new ProcessResult();
            if (_visitedNodes.Contains(node))
            {
                return result;
            }
            
            result = processor.Process(node, context, settings);
            _visitedNodes.Add(node);

            foreach (var child in node.Children())
            {
                if (child.IsFhirResource())
                {
                    continue;
                }

                result.Update(ProcessNodeRecursive((ElementNode)child, processor, context, settings));
            }

            return result;
        }
    }
}
