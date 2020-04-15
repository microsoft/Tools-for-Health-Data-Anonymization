using System;
using System.Collections.Generic;
using System.Text;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Rest;

namespace Fhir.Anonymizer.Core.Visitors
{
    public class ProcessorVisitor : AbstractElementNodeVisitor
    {
        private IAnonymizerProcessor _processor;
        private HashSet<ElementNode> _visitedNodes;

        public ProcessResult ProcessResult { get; set; } = new ProcessResult();

        public ProcessorVisitor(IAnonymizerProcessor processor, HashSet<ElementNode> visitedNodes)
        {
            _processor = processor;
            _visitedNodes = visitedNodes;
        }

        public override bool PreVisitBundleEntryNode(ElementNode node)
        {
            // Skip process in the bundle entry
            return false;
        }

        public override bool PreVisitContainedNode(ElementNode node)
        {
            // Skip process in the contained entry
            return false;
        }

        public override bool Visit(ElementNode node)
        {
            if (_visitedNodes.Contains(node))
            {
                return false;
            }

            var result = _processor.Process(node);
            _visitedNodes.Add(node);
            ProcessResult.Update(result);

            return true;
        }
    }
}
