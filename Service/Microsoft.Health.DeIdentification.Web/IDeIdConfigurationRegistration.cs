// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdConfigurationRegistration
    {
        public DeIdConfiguration GetByName(string name);
    }
}
