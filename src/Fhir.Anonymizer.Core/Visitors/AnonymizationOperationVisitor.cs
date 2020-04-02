using System;
using System.Collections.Generic;
using System.Text;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public class AnonymizationOperationVisitor : AbstractElementNodeVisitor<AnonymizationVisitContext>
    {
        private IAnonymizerProcessor _processor;

        public AnonymizationOperationVisitor(IAnonymizerProcessor processor)
        {
            _processor = processor;
        }

        public override bool Visit(ElementNode node, AnonymizationVisitContext context)
        {
            if (context.VisitedNodes.Contains(node))
            {
                return false;
            }

            _processor.Process(node);
            context.VisitedNodes.Add(node);

            return true;
        }
    }
}
