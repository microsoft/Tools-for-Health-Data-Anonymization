using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Batch.Models
{
    public class BatchInputData
    {
        public string dataSourceType { get; set; }

        public List<Dictionary<string, string>> sourceDataset { get; set; }

        public DestinationDataset destinationDataset { get; set; }

        public string deIdConfiguration { get; set; }
    }
}
