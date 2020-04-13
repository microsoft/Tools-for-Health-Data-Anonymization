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
        public static void Accept<T>(this ElementNode node, AbstractElementNodeVisitor<T> visitor, T context)
        {
            bool shouldVisitChild = visitor.Visit(node, context);

            if (shouldVisitChild)
            {
                foreach (var child in node.Children().Cast<ElementNode>())
                {
                    // skip nodes in Bundle & Contained
                    if (child.IsEntryNode() || child.IsContainedNode())
                    {
                        continue;
                    }

                    child.Accept<T>(visitor, context);
                }
            }

            visitor.EndVisit(node, context);
        }
    }
}
