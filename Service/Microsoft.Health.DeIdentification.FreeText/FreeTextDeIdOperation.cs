// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdOperation : IDeIdOperation<string, string>
    {
        public string Process(string source)
        {
            throw new NotImplementedException();
        }
    }
}