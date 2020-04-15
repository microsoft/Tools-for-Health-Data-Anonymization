using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core
{
    public class InternalAnonymizeLogic
    {
        private AnonymizationFhirPathRule[] _rules;
        private Dictionary<string, IAnonymizerProcessor> _processors;

        public InternalAnonymizeLogic(AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _rules = rules;
            _processors = processors;
        }

        public Resource Anonymize(Resource resource)
        {
            ElementNode node = ElementNode.FromElement(resource.ToTypedElement());
            var subResourceNodesAndSelf = node.SubResourceNodesAndSelf();
            AnonymizationVisitContext context = new AnonymizationVisitContext();
            
            foreach (var subNode in subResourceNodesAndSelf)
            {
                string typeString = subNode.InstanceType;
                var resourceSpecificAndGeneralRules = _rules.Where(r => r.ResourceType.Equals(typeString) || string.IsNullOrEmpty(r.ResourceType));
                
                foreach (var rule in resourceSpecificAndGeneralRules)
                {
                    string method = rule.Method.ToUpperInvariant();
                    if (!_processors.ContainsKey(method))
                    {
                        continue;
                    }

                    ProcessNode(context, subNode, rule, method);
                }
            }

            node.RemoveNullChildren();
            Resource result = node.ToPoco<Resource>();
            result.TryAddSecurityLabels(context.ProcessResult);
            return result;
        }

        private void ProcessNode(AnonymizationVisitContext context, ElementNode subNode, AnonymizationFhirPathRule rule, string method)
        {
            //var visitor = new ProcessorVisitor(_processors[method]);
            //foreach (var matchNode in subNode.Select(rule.Expression).Cast<ElementNode>())
            //{
            //    matchNode.Accept(visitor, context);
            //}
        }
    }
}
