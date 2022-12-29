// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdBatchProcessor : BatchProcessor<ResourceList, ResourceList>
    {
        private IDeIdOperation<ResourceList, ResourceList> _operation;

        public FhirDeIdBatchProcessor(IDeIdOperation<ResourceList, ResourceList> operation)
        {
            _operation = operation;
        }

        public override ResourceList[] BatchProcessFunc(BatchInput<ResourceList> input)
        {
            var inputFileNames = input.Sources.Select(source => source.inputFileName).ToArray();
            var outputFileNames = input.Sources.Select(source => source.outputFileName).ToArray();
            var result = input.Sources.Select(source => _operation.Process(source)).ToArray();
            for (int idx=0; idx < result.Length; idx++)
            {
                result[idx].inputFileName = inputFileNames[idx];
                result[idx].outputFileName = outputFileNames[idx];
            }
            return result;
        }
    }
}