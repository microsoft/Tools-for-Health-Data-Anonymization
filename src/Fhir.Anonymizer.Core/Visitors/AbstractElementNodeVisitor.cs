using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Visitors
{
    public abstract class AbstractElementNodeVisitor
    {
        public virtual async Task<bool> Visit(ElementNode node)
        {
            return true;
        }

        public virtual void EndVisit(ElementNode node)
        {
            
        }
    }
}
