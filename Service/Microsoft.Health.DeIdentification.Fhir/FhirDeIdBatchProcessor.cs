// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir.Model;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdBatchProcessor : BatchProcessor<BatchFhirDataContext, BatchFhirDataContext>
    {
        private IDeIdOperation<StringBatchData, StringBatchData> _operation;

        public FhirDeIdBatchProcessor(IDeIdOperation<StringBatchData, StringBatchData> operation)
        {
            _operation = operation;
        }

        public override BatchFhirDataContext[] BatchProcessFunc(BatchInput<BatchFhirDataContext> input)
        {
            var inputFileNames = input.Sources.Select(source => source.InputFileName).ToArray();
            var outputFileNames = input.Sources.Select(source => source.OutputFileName).ToArray();
            var resources = input.Sources.Select(source => _operation.Process(source.Resources)).ToArray();
            var result = new List<BatchFhirDataContext>();
            for (int idx=0; idx < resources.Length; idx++)
            {
                result.Add(new BatchFhirDataContext()
                {
                    Resources = resources[idx],
                    InputFileName = inputFileNames[idx],
                    OutputFileName = outputFileNames[idx],
                });
            }
            return result.ToArray();
        }
    }
}