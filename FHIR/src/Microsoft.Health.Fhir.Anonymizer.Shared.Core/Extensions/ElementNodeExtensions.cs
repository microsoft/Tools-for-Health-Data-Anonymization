using System;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeExtensions
    {
        public static bool IsAgeDecimalNode(this ElementNode node)
        {
            return node != null &&
                   node.Parent.IsAgeNode() &&
                   string.Equals(node.InstanceType, Constants.DecimalTypeName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsReferenceStringNode(this ElementNode node)
        {
            return node != null &&
                   node.Parent.IsReferenceNode() &&
                   string.Equals(node.Name, Constants.ReferenceStringNodeName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
