using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;

namespace Fhir.Anonymizer.Core.Visitors
{
    public class TypeAnonymizationRuleVisitor : AbstractAnonymizationVisitor
    {
        private Stack<Context> _contextStack = new Stack<Context>();

        public TypeAnonymizationRuleVisitor()
        {
            _contextStack.Push(new Context());
        }

        public override bool Visit(ElementNode node)
        {
            Context context = _contextStack.Peek();
            context.Path.Add(node);

            AnonymizationExtendTypeRule rule = MatchTypeRule(context.Path);
            if (context.InheritRules.Count > 0 && context.InheritRules.Peek().Priority > rule.Priority)
            {
                rule = context.InheritRules.Peek();
            }
            context.InheritRules.Push(rule);

            return true;
        }

        public override void EndVisit(ElementNode node)
        {
            Context context = _contextStack.Peek();
            context.Path.Remove(node);
            AnonymizationExtendTypeRule rule = context.InheritRules.Pop();

            Process(node, rule);
        }

        public override void PreVisitContainedNode(ElementNode node)
        {
            _contextStack.Push(new Context());
        }

        public override void PreVisitEntryNode(ElementNode node)
        {
            _contextStack.Push(new Context());
        }

        public override void PostVisitContainedNode(ElementNode node)
        {
            _contextStack.Pop();
        }

        public override void PostVisitEntryNode(ElementNode node)
        {
            _contextStack.Pop();
        }

        private AnonymizationExtendTypeRule MatchTypeRule(List<ElementNode> path)
        {
            throw new NotImplementedException();
        }

        private void Process(ElementNode node, AnonymizationExtendTypeRule rule)
        {
            throw new NotImplementedException();
        }

        private class Context
        {
            public List<ElementNode> Path { get; set; } = new List<ElementNode>();
            public Stack<AnonymizationExtendTypeRule> InheritRules { get; set; } = new Stack<AnonymizationExtendTypeRule>();
        }
    }

}
