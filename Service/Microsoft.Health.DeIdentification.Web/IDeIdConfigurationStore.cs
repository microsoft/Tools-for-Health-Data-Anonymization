using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdConfigurationStore
    {
        public DeIdConfiguration GetByName(string name);
    }
}
