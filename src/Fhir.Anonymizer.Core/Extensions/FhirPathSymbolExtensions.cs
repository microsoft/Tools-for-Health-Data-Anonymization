using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath.Expressions;

namespace Fhir.Anonymizer.Core.Extensions
{
    public static class FhirPathSymbolExtensions
    {
        public static SymbolTable AddExtensionSymbols(this SymbolTable t)
        {
            t.Add("nodesByType", (IEnumerable<ITypedElement> f, string typeName) => NodesByType(f, typeName), doNullProp: true);
            t.Add("nodesByName", (IEnumerable<ITypedElement> f, string name) => NodesByName(f, name), doNullProp: true);

            return t;
        }

        public static IEnumerable<ITypedElement> NodesByType(IEnumerable<ITypedElement> nodes, string typeName)
        {
            return nodes.DescendantsAndSelf().Where(n => typeName.Equals(n.InstanceType));
        }

        public static IEnumerable<ITypedElement> NodesByName(IEnumerable<ITypedElement> nodes, string name)
        {
            return nodes.DescendantsAndSelf().Where(n => name.Equals(n.Name));
        }
    }
}
