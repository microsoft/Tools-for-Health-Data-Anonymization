﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdBatchProcessor : BatchProcessor<string, string>
    {
        private IDeIdOperation<string, string> _operation;

        public FreeTextDeIdBatchProcessor(IDeIdOperation<string, string> operation)
        {
            _operation = operation;
        }

        public override string[] BatchProcessFunc(BatchInput<string> input)
        {
            return input.Sources.Select(source => _operation.Process(source)).ToArray();
        }
    }
}
