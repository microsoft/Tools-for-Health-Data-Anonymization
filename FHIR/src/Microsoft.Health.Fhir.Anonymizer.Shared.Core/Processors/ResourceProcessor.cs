using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class ResourceProcessor : IAnonymizerProcessor
    {
        private readonly AnonymizationFhirPathRule[] _rules;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<ResourceProcessor>();

        private readonly HashSet<ElementNode> _visitedNodes = new HashSet<ElementNode>();
        private readonly Dictionary<string, List<ITypedElement>> _typeToNodeLookUp = new Dictionary<string, List<ITypedElement>>();
        private readonly Dictionary<string, List<ITypedElement>> _nameToNodeLookUp = new Dictionary<string, List<ITypedElement>>();

        private static readonly PocoStructureDefinitionSummaryProvider s_provider = new PocoStructureDefinitionSummaryProvider();
        private const string _metaNodeName = "meta";

        public ResourceProcessor(AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _rules = rules;
            _processors = processors;
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            // Initialize cache for every resource node
            InitializeNodeCache(node);

            var result = new ProcessResult();
            var resourceRules = GetRulesByType(node.InstanceType);

            foreach (var rule in resourceRules)
            {
                var ruleResult = new ProcessResult();
                var method = rule.Method.ToUpperInvariant();
                var ruleContext = new ProcessContext
                {
                    VisitedNodes = _visitedNodes
                };

                if (!_processors.ContainsKey(method))
                {
                    continue;
                }

                var matchNodes = GetMatchNodes(rule, node);

                foreach (var matchNode in matchNodes)
                {
                    ruleResult.Update(ProcessNodeRecursive((ElementNode) matchNode.ToElement(), _processors[method], ruleContext, rule.RuleSettings));
                }

                LogProcessResult(node, rule, ruleResult);

                result.Update(ruleResult);
            }

            return result;
        }

        public void AddSecurityTag(ElementNode node, ProcessResult result)
        {
            if (node == null || result.ProcessRecords.Count == 0)
            {
                return;
            }

            var metaNode = (ElementNode)node.GetMeta();
            var meta = metaNode?.ToPoco<Meta>() ?? new Meta();

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
                string.Equals(x.Code, SecurityLabels.MASKED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.MASKED);
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

            var newMetaNode = ElementNode.FromElement(meta.ToTypedElement());
            if (metaNode == null)
            {
                node.Add(s_provider, newMetaNode, _metaNodeName);
            }
            else
            {
                node.Replace(s_provider, metaNode, newMetaNode);
            }
        }

        private void InitializeNodeCache(ITypedElement node)
        {
            _typeToNodeLookUp.Clear();
            _nameToNodeLookUp.Clear();

            InitializeNodeCacheRecursive(node);
        }

        private void InitializeNodeCacheRecursive(ITypedElement node)
        {
            foreach (var child in node.Children())
            {
                // Cache instance type
                if (_typeToNodeLookUp.ContainsKey(child.InstanceType))
                {
                    _typeToNodeLookUp[child.InstanceType].Add(child);
                }
                else
                {
                    _typeToNodeLookUp[child.InstanceType] = new List<ITypedElement> { child };
                }

                // Cache name
                if (_nameToNodeLookUp.ContainsKey(child.Name))
                {
                    _nameToNodeLookUp[child.Name].Add(child);
                }
                else
                {
                    _nameToNodeLookUp[child.Name] = new List<ITypedElement> { child };
                }

                // Recursively process node's children except resource children, which will be processed independently as a resource
                if (!child.IsFhirResource())
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
            var typeMatch = AnonymizationFhirPathRule.TypeRuleRegex.Match(rule.Path);
            var nameMatch = AnonymizationFhirPathRule.NameRuleRegex.Match(rule.Path);

            if (typeMatch.Success)
            {
                return GetMatchNodesFromLookUp(_typeToNodeLookUp, typeMatch.Groups["type"].Value, typeMatch.Groups["expression"].Value);
            }

            if (nameMatch.Success)
            {
                return GetMatchNodesFromLookUp(_nameToNodeLookUp, nameMatch.Groups["name"].Value, nameMatch.Groups["expression"].Value);
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

        public ProcessResult ProcessNodeRecursive(ElementNode node, IAnonymizerProcessor processor, ProcessContext context, Dictionary<string, object> settings)
        {
            var result = new ProcessResult();
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
