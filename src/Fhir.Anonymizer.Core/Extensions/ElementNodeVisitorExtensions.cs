using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeVisitorExtensions
    {
        public static void Accept(this ElementNode node, AbstractElementNodeVisitor visitor)
        {
            bool shouldVisitChild = visitor.Visit(node);

            if (shouldVisitChild)
            {
                foreach (var child in node.Children().Cast<ElementNode>())
                {
                    if (child.IsContainedNode())
                    {
                        VisitContainedNode(visitor, child);
                    }
                    else if (child.IsEntryResourceNode())
                    {
                        VisitBundleEntryResourceNode(visitor, child);
                    }
                    else
                    {
                        VisitBasicNode(visitor, child);
                    }
                }
            }

            visitor.EndVisit(node);
        }

        private static void VisitBasicNode(AbstractElementNodeVisitor visitor, ElementNode child)
        {
            child.Accept(visitor);
        }

        private static void VisitBundleEntryResourceNode(AbstractElementNodeVisitor visitor, ElementNode child)
        {
            bool shouldVisitEntryResourceNode = visitor.PreVisitBundleEntryNode(child);
            if (shouldVisitEntryResourceNode)
            {
                child.Accept(visitor);
            }
            visitor.PostVisitBundleEntryNode(child);
        }

        private static void VisitContainedNode(AbstractElementNodeVisitor visitor, ElementNode child)
        {
            bool shouldVisitContainedNode = visitor.PreVisitContainedNode(child);
            if (shouldVisitContainedNode)
            {
                child.Accept(visitor);
            }
            visitor.PostVisitContainedNode(child);
        }
    }
}
