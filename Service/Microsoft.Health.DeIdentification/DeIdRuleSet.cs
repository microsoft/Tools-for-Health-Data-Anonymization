using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Contract
{
    public class DeIdRuleSet
    {
        public string Format { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }
    }
}
