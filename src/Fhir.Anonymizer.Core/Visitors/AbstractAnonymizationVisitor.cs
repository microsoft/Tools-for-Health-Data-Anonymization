using System;
using System.Collections.Generic;
using System.Text;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public abstract class AbstractAnonymizationVisitor
    {
        public virtual bool Visit(ElementNode node)
        {
            return true;
        }

        public virtual void EndVisit(ElementNode node)
        {
            
        }

        public virtual void PreVisitContainedNode(ElementNode node)
        {
            
        }

        public virtual void PostVisitContainedNode(ElementNode node)
        {

        }

        public virtual void PreVisitEntryNode(ElementNode node)
        {

        }

        public virtual void PostVisitEntryNode(ElementNode node)
        {

        }
    }
}
