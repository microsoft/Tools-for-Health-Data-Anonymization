using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Fhir.Anonymizer.Shared.Core.Models
{
    public class EmptyElement: ITypedElement
    {
        public EmptyElement(string type)
        {
            InstanceType = type;
        }
        private static Meta _meta = new Meta() { Security = new List<Coding>() { SecurityLabels.REDACT } };

        private static IStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();

        private static FhirJsonParser _parser = new FhirJsonParser();

        protected List<ITypedElement> ChildList = new List<ITypedElement>() { _meta.ToTypedElement("meta") };
        public string Name => "empty";

        public string InstanceType { get; set; }

        public object Value => null;

        public string Location => Name;

        public IElementDefinitionSummary Definition => ElementDefinitionSummary.ForRoot(_provider.Provide(InstanceType));

        public IEnumerable<ITypedElement> Children(string? name = null) => 
            name == null ? ChildList : ChildList.Where(c => c.Name.MatchesPrefix(name));

        public static bool IsEmptyElement(ITypedElement element)
        {
            if(element.Children().Count() ==1 && element.Children("meta").Count() == 1)
            {
                return true;
            }
            return false;
        }

        public static bool IsEmptyElement(string elementJson)
        {
            try
            {
                var element = _parser.Parse(elementJson).ToTypedElement();
                return IsEmptyElement(element);
            }
            catch
            {
                return false;
            }
        }
    }
}
