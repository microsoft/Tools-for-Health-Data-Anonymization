using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
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
            var pathRules = configurationManager.GetPathRulesByResourceType(root.InstanceType); 
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
            var maxPriority = int.MaxValue;

            var currentNode = node;
            var typePath = string.Empty;
            do
            {
                var path = string.IsNullOrEmpty(typePath) ? currentNode.InstanceType : $"{currentNode.InstanceType}.{typePath}";
                var ruleForPath = _typeRuleMap.GetValueOrDefault(path, null);

                if (ruleForPath != null && ruleForPath.Priority < maxPriority) 
                {
                    rule = ruleForPath;
                    maxPriority = rule.Priority;
                }

                typePath = string.IsNullOrEmpty(typePath) ? currentNode.Name : $"{currentNode.Name}.{typePath}";
                currentNode = currentNode.Parent;
            } while (currentNode != null);
            
            return rule;
        }
    }
}
