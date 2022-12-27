// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using System.Threading.Channels;

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
