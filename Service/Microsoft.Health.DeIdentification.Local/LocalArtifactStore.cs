using Microsoft.Health.DeIdentification.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Local
{
    public class LocalArtifactStore : IArtifactStore
    {
        public TContent ResolveArtifact<TContent>(string reference)
        {
            throw new NotImplementedException();
        }
    }
}
