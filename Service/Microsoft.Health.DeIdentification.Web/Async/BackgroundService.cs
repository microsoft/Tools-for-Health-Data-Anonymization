// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
