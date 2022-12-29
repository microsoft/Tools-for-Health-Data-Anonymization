using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Batch.Models
{
    public class DestinationDataset
    {
        public string store { get; set; }

        public string name { get; set; }

        public string account { get; set; }

        public string folderPath { get; set; }

        public Authentication authentication { get; set; }
    }
}
