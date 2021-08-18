using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Visitors
{
    public class AnonymizationVisitor : AbstractElementNodeVisitor
    {
        private readonly ResourceProcessor _resourceProcessor;
        private readonly Stack<Tuple<ElementNode, ProcessResult>> _contextStack = new Stack<Tuple<ElementNode, ProcessResult>>();

        public bool AddSecurityTag { get; set; } = true;

        public AnonymizationVisitor(AnonymizationFhirPathRule[] rules, Dictionary<string, IAnonymizerProcessor> processors)
        {
            _resourceProcessor = new ResourceProcessor(rules, processors);
        }

        public override bool Visit(ElementNode node)
        {
            if (node.IsFhirResource())
            {
                var result = _resourceProcessor.Process(node);
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
                    _resourceProcessor.AddSecurityTag(node, result);
                }
            }
        }
    }
}
