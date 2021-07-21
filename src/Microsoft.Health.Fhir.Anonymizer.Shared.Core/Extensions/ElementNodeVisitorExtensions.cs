using System.Linq;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Visitors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeVisitorExtensions
    {
        public static void Accept(this ElementNode node, AbstractElementNodeVisitor visitor)
        {
            bool shouldVisitChild = visitor.Visit(node);

            if (shouldVisitChild)
            {
                // If an ElementNode is created by ElementNode.FromElement(), its children are of type ElementNode
                // Cast them to ElementNode directly
                // https://github.com/FirelyTeam/firely-net-common/blob/master/src/Hl7.Fhir.ElementModel/ElementNode.cs
                foreach (var child in node.Children().Cast<ElementNode>())
                {
                    child.Accept(visitor);
                }
            }

            visitor.EndVisit(node);
        }
    }
}
