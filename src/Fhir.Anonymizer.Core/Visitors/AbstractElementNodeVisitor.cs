using System;
using System.Collections.Generic;
using System.Text;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public abstract class AbstractElementNodeVisitor
    {
        public virtual bool PreVisitContainedNode(ElementNode node)
        {
            return true;
        }

        public virtual void PostVisitContainedNode(ElementNode node)
        {
            
        }

        public virtual bool PreVisitBundleEntryNode(ElementNode node)
        {
            return true;
        }

        public virtual void PostVisitBundleEntryNode(ElementNode node)
        {

        }

        public virtual bool Visit(ElementNode node)
        {
            return true;
        }

        public virtual void EndVisit(ElementNode node)
        {
            
        }
    }
}
