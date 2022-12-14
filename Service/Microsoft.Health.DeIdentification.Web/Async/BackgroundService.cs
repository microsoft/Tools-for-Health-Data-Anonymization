using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web.Async
{
    public class BackgroundService
    {
        Task<string> StartAsync(string content)
        {
            // start job hosting
            // We can leverage job hosting here for long running job
            throw new NotImplementedException();
        }
    }
}
