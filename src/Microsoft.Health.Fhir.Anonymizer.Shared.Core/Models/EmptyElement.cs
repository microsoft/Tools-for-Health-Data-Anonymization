using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using System;
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
        public string Name => "bundle";

        public string InstanceType { get; set; }

        public object Value => null;

        public string Location => Name;

        public IElementDefinitionSummary Definition => null;

        public IEnumerable<ITypedElement> Children(string? name = null) => new List<ITypedElement>() { _meta.ToTypedElement()};
    }
}
