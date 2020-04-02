using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;

namespace Fhir.Anonymizer.Core
{
    internal class InternalAnonymizeLogic
    {
        private AnonymizerRule[] _rules;
        private Dictionary<string, IAnonymizerProcessor> _processors;

        internal InternalAnonymizeLogic(AnonymizerRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _rules = rules;
            _processors = processors;
        }

        internal ElementNode Anonymize(ElementNode node)
        {
            var subResourceNodesAndSelf = node.SubResourceNodesAndSelf();

            string typeString = node.InstanceType;
            var resourceSpecificAndGeneralRules = _rules.Where(r => r.ResourceType.Equals(typeString) || string.IsNullOrEmpty(r.ResourceType));

            AnonymizationVisitContext context = new AnonymizationVisitContext();
            foreach (var subNode in subResourceNodesAndSelf)
            {
                foreach (var rule in resourceSpecificAndGeneralRules)
                {
                    string method = rule.Method.ToUpperInvariant();
                    if (!_processors.ContainsKey(method))
                    {
                        continue;
                    }

                    var visitor = new AnonymizationOperationVisitor(_processors[method]);
                    foreach (var matchNode in subNode.Select(rule.Expression).Cast<ElementNode>())
                    {
                        matchNode.Accept(visitor, context);
                    }
                }
            }

            return node;
        }
    }
}
