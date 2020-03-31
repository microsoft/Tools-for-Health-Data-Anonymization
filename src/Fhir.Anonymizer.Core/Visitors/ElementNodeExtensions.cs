using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public static class ElementNodeExtensions
    {
        public static void Accept(this ElementNode node, AbstractAnonymizationVisitor visitor)
        {
            bool shouldVisitChild = visitor.Visit(node);

            if (shouldVisitChild)
            {
                IEnumerable<ElementNode> childrenWithoutEntryNodes = node.Children().Where(e => !ElementNodeExtension.s_entryNodeName.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase)).Cast<ElementNode>();
                foreach (var child in childrenWithoutEntryNodes)
                {
                    bool isContainedNode = child.IsContainedNode();
                    if (isContainedNode)
                    {
                        visitor.PreVisitContainedNode(child);
                    }

                    child.Accept(visitor);

                    if (isContainedNode)
                    {
                        visitor.PostVisitContainedNode(child);
                    }
                }

                if (node.IsBundleNode())
                {
                    IEnumerable<ElementNode> entryResourceChildren = node.GetEntryResourceChildren();
                    foreach (var child in entryResourceChildren)
                    {
                        visitor.PreVisitEntryNode(child);
                        child.Accept(visitor);
                        visitor.PostVisitEntryNode(child);
                    }
                }
            }

            visitor.EndVisit(node);
        }
    }
}
