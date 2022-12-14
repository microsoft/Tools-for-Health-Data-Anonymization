﻿using Microsoft.Health.DeIdentification.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Azure
{
    // This loader can be used for FHIR resource and free text
    public class AzureBlobTextLoader : DataLoader<string>
    {
        protected override Task LoadDataInternalAsync(Channel<string> outputChannel, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
