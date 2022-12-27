// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;

namespace Microsoft.Health.DeIdentification.Azure
{
    public class AzureBlobTextWriter : DataWriter<string, string>
    {
        public override string[] BatchProcessFunc(BatchInput<string> input)
        {
            // Write block to blob and return new string[] { "Progress Message" }
            throw new NotImplementedException();
        }

        protected override Task CommitAsync(CancellationToken cancellationToken)
        {
            // Commit all block id
            throw new NotImplementedException();
        }
    }
}
