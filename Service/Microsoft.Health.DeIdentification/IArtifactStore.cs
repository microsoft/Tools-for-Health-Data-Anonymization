// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IArtifactStore
    {
        public TContent ResolveArtifact<TContent>(string reference);
    }
}