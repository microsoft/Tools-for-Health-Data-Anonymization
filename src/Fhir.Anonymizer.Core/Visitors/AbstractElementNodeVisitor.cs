using System;
using System.Collections.Generic;
using System.Text;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public abstract class AbstractElementNodeVisitor<T>
    {
        public virtual bool Visit(ElementNode node, T context)
        {
            return true;
        }

        public virtual void EndVisit(ElementNode node, T context)
        {
            
        }
    }
}
