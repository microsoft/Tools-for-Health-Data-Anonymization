using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class ResourceAnonymizerContext
    {
        public IEnumerable<AnonymizerRule> RuleList { get; set; }

        public HashSet<string> PathSet { get; set; }

        public ResourceAnonymizerContext(IEnumerable<AnonymizerRule> ruleList)
        {
            RuleList = ruleList;
            PathSet = ruleList.Select(rule => rule.Path).ToHashSet();
        }

        public static ResourceAnonymizerContext Create(ElementNode root, AnonymizerConfigurationManager configurationManager)
        {
            var rules = new List<AnonymizerRule>(configurationManager.GetPathRulesByResourceType(root.InstanceType));

            var typeRules = configurationManager.GetTypeRules();
            if (typeRules != null && typeRules.Any())
            {
                var rulePaths = rules.Select(rule => rule.Path).ToHashSet();
                TransformTypeRulesToPathRules(root, typeRules, rules, rulePaths, new HashSet<string>());
            }

            return new ResourceAnonymizerContext(rules);
        }

        private static void TransformTypeRulesToPathRules(ElementNode node, Dictionary<string, string> typeRules, List<AnonymizerRule> rules, HashSet<string> rulePaths, HashSet<string> generatedTypePaths)
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

            if (!generatedTypePaths.Contains(path) && typeRules.ContainsKey(node.InstanceType))
            {
                var rule = new AnonymizerRule(path, typeRules[node.InstanceType], AnonymizerRuleType.TypeRule, node.InstanceType);
                rules.Add(rule);
                generatedTypePaths.Add(rule.Path);
            }

            var children = node.Children().Cast<ElementNode>();
            foreach (var child in children)
            {
                TransformTypeRulesToPathRules(child, typeRules, rules, rulePaths, generatedTypePaths);
            }
        }
    }
}
