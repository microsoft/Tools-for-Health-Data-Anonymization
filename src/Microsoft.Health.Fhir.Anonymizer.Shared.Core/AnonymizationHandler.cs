using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Visitors
{
    public class AnonymizationHandler
    {
        private readonly AnonymizationFhirPathRule[] _rules;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;
        private HashSet<ITypedElement> _visitedNodes = new HashSet<ITypedElement>();
        private Dictionary<string, List<ITypedElement>> _typeToNodeCache = new Dictionary<string, List<ITypedElement>>();
        private Dictionary<string, List<ITypedElement>> _nameToNodeCache = new Dictionary<string, List<ITypedElement>>();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizationHandler>();

        public bool AddSecurityTag { get; set; } = true;

        public AnonymizationHandler(AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _rules = rules;
            _processors = processors;
        }

        public ProcessResult Handle(ITypedElement node)
        {
            var result = new ProcessResult();
            
            if (node.IsBundleNode())
            {
                var entries = node.GetEntryResourceChildren();
                foreach (var entry in entries)
                {
                    var entryResult = AnonymizeResourceNode(entry);
                    result.Update(entryResult);
                    if (AddSecurityTag)
                    {
                        entry.AddSecurityTag(entryResult);
                        _visitedNodes.UnionWith(entry.Children("meta").Children("security").DescendantsAndSelf());
                    }
                }
            }

            var resourceResult = AnonymizeResourceNode(node);
            result.Update(resourceResult);
            if (AddSecurityTag)
            {
                node.AddSecurityTag(result);
                _visitedNodes.UnionWith(node.Children("meta").Children("security").DescendantsAndSelf());
            }

            return result;
        }

        private ProcessResult AnonymizeResourceNode(ITypedElement node)
        {
            InitializeNodeCache(node);

            var result = new ProcessResult();
            var resourceRules = GetRulesByType(node.InstanceType);

            foreach (var rule in resourceRules)
            {
                var method = rule.Method.ToUpperInvariant();
                if (!_processors.ContainsKey(method))
                {
                    throw new AnonymizerProcessingException($"The anonymization method for rule {rule.Source} is not supported: {method}");
                }

                var matchNodes = GetMatchNodes(rule, node);
                var resultOnRule = new ProcessResult();
                var context = new ProcessContext
                {
                    VisitedNodes = _visitedNodes
                };

                foreach (var matchNode in matchNodes)
                {
                    var matchElementNode = matchNode.ToElement();
                    if (_visitedNodes.Contains(matchElementNode))
                    {
                        continue;
                    }

                    resultOnRule.Update(_processors[method].Process((ElementNode)matchElementNode, context, rule.RuleSettings));
                    _visitedNodes.UnionWith(matchElementNode.DescendantsAndSelf());
                }

                result.Update(resultOnRule);

                LogProcessResult(node, rule, resultOnRule);
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
                // Cache node's instance type
                if (_typeToNodeCache.ContainsKey(child.InstanceType))
                {
                    _typeToNodeCache[child.InstanceType].Add(child);
                }
                else
                {
                    _typeToNodeCache[child.InstanceType] = new List<ITypedElement> { child };
                }

                // Cache node's name
                if (_nameToNodeCache.ContainsKey(child.Name))
                {
                    _nameToNodeCache[child.Name].Add(child);
                }
                else
                {
                    _nameToNodeCache[child.Name] = new List<ITypedElement> { child };
                }

                // Recursively process node's children except Bundle's entry children
                if (!child.IsEntryNode())
                {
                    InitializeNodeCacheRecursive(child);
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

        private IEnumerable<ITypedElement> GetMatchNodes(AnonymizationFhirPathRule rule, ITypedElement node)
        {
            var matchNodes = new List<ITypedElement>();
            var resourceTypeMatch = AnonymizationFhirPathRule.TypeRuleRegex.Match(rule.Path);
            var nameMatch = AnonymizationFhirPathRule.NameRuleRegex.Match(rule.Path);

            if (resourceTypeMatch.Success)
            {
                var resourceType = resourceTypeMatch.Groups["resourceType"].Value;
                if (_typeToNodeCache.ContainsKey(resourceType))
                {
                    var expression = resourceTypeMatch.Groups["expression"].Value;
                    if (!string.IsNullOrEmpty(expression))
                    {
                        var typeNodes = _typeToNodeCache[resourceType];
                        matchNodes.AddRange(typeNodes.Children(expression));
                    }
                    else
                    {
                        matchNodes = _typeToNodeCache[resourceType];
                    }
                }
            }
            else if (nameMatch.Success)
            {
                var name = nameMatch.Groups["name"].Value;
                if (_nameToNodeCache.ContainsKey(name))
                {
                    var expression = nameMatch.Groups["expression"].Value;
                    if (!string.IsNullOrEmpty(expression))
                    {
                        var nameNodes = _nameToNodeCache[name];
                        matchNodes.AddRange(nameNodes.Children(expression));
                    }
                    else
                    {
                        matchNodes = _nameToNodeCache[name];
                    }

                }
            }
            else if (rule.IsResourceTypeRule)
            {
                /*
                * Special case handling:
                * Senario: FHIR path only contains resourceType: Patient, Resource. 
                * Sample AnonymizationFhirPathRule: { "path": "Patient", "method": "keep" }
                * 
                * Current FHIR path lib do not support navigate such ResourceType FHIR path from resource in bundle.
                * Example: navigate with FHIR path "Patient" from "Bundle.entry[0].resource[0]" is not support
                */
                matchNodes = new List<ITypedElement> { node };
            }
            else
            {
                matchNodes = node.Select(rule.Expression).ToList();
            }

            return matchNodes;
        }

        private void LogProcessResult(ITypedElement node, AnonymizationFhirPathRule rule, ProcessResult resultOnRule)
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
    }
}
