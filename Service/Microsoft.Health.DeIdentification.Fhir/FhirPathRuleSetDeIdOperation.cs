// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirPathRuleSetDeIdOperation : IDeIdOperation<StringBatchData, StringBatchData>
    {
        private readonly AnonymizerEngine _anonymizerEngine;

        public FhirPathRuleSetDeIdOperation(AnonymizerEngine anonymizerEngine)
        {
            _anonymizerEngine = EnsureArg.IsNotNull(anonymizerEngine, nameof(anonymizerEngine));
        }

        public StringBatchData Process(StringBatchData source)
        {
            var result = new StringBatchData();
            ;
            foreach (var item in source.Resources)
            {
                result.Resources.Add(ProcessSingle(item));
            }

            return result;
        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }
    }
}