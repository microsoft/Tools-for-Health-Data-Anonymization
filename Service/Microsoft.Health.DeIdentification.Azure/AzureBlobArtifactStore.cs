// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Azure
{
    public class AzureBlobArtifactStore : IArtifactStore
    {
        public TContent ResolveArtifact<TContent>(string reference)
        {
            throw new NotImplementedException();
        }
    }
}