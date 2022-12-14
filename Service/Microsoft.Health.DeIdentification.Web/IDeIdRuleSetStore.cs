using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdRuleSetStore
    {
        public DeIdRuleSet GetByName(string name);
    }
}
