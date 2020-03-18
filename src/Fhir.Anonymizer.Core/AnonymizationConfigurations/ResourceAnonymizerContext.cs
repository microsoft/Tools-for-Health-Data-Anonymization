using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class ResourceAnonymizerContext
    {
        private string _resourceId;
        private Dictionary<string, AnonymizerRule> _pathRuleMap;
        private Dictionary<string, AnonymizerRule> _typeRuleMap;

        public ResourceAnonymizerContext(string resourceId, IEnumerable<AnonymizerRule> pathRuleList, IEnumerable<AnonymizerRule> typeRuleList)
        {
            _resourceId = resourceId;
            _pathRuleMap = pathRuleList.ToDictionary(rule => rule.Path, rule => rule);
            _typeRuleMap = typeRuleList.ToDictionary(rule => rule.Path, rule => rule);
        }

        public static ResourceAnonymizerContext Create(ElementNode root, AnonymizerConfigurationManager configurationManager)
        {
            var pathRules = ResolveGenericFhirPathToBasicFhirPath(root, configurationManager.GetPathRulesByResourceType(root.InstanceType));
           
            var typeRules = configurationManager.GetTypeRules();
            return new ResourceAnonymizerContext(root.GetNodeId(), pathRules, typeRules);
        }

        public string GetResourceId()
        {
            return _resourceId;
        }

        public AnonymizerRule GetNodePathRule(ElementNode node)
        {
            return _pathRuleMap.GetValueOrDefault(node.GetFhirPath(), null);
        }

        public AnonymizerRule GetNodeTypeRule(ElementNode node)
        {
            AnonymizerRule rule = null;
            var currentNode = node;
            var typePath = string.Empty;
            do
            {
                var path = string.IsNullOrEmpty(typePath) ? currentNode.InstanceType : $"{currentNode.InstanceType}.{typePath}";
                rule = _typeRuleMap.GetValueOrDefault(path, rule);

                typePath = string.IsNullOrEmpty(typePath) ? currentNode.Name : $"{currentNode.Name}.{typePath}";
                currentNode = currentNode.Parent;
            } while (currentNode != null);
            
            return rule;
        }

        private static IEnumerable<AnonymizerRule> ResolveGenericFhirPathToBasicFhirPath(ElementNode root, IEnumerable<AnonymizerRule> genericFhirPathRules)
        {
            var basicRules = new List<AnonymizerRule>();
            foreach(var rule in genericFhirPathRules)
            {
                var matchedNodes = root.Select(rule.Path).Cast<ElementNode>();
                basicRules.AddRange(matchedNodes.Select(node => new AnonymizerRule(node.GetFhirPath(), rule.Method, rule.Type, rule.Source)));
            }

            return basicRules;
        }

        private static void TransformTypeRulesToPathRules(ElementNode node, Dictionary<string, string> typeRules, List<AnonymizerRule> rules, HashSet<string> rulePaths)
        {
            if (node.IsContainedNode() || node.IsEntryNode())
            {
                return;
            }

            string path = node.GetFhirPath();
            if (rulePaths.Contains(path))
            {
                return;
            }

            if (typeRules.ContainsKey(node.InstanceType))
            {
                var rule = new AnonymizerRule(path, typeRules[node.InstanceType], AnonymizerRuleType.TypeRule, node.InstanceType);
   
                rules.Add(rule);
                rulePaths.Add(rule.Path);
            }

            var children = node.Children().Cast<ElementNode>();
            foreach (var child in children)
            {
                TransformTypeRulesToPathRules(child, typeRules, rules, rulePaths);
            }
        }
    }
}
