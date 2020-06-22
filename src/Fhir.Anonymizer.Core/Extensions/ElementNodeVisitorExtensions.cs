using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeVisitorExtensions
    {
        public static async Task Accept(this ElementNode node, AbstractElementNodeVisitor visitor)
        {
            bool shouldVisitChild = await visitor.Visit(node);

            if (shouldVisitChild)
            {
                foreach (var child in node.Children().Cast<ElementNode>())
                {
                    child.Accept(visitor);
                }
            }

            visitor.EndVisit(node);
        }
    }
}
