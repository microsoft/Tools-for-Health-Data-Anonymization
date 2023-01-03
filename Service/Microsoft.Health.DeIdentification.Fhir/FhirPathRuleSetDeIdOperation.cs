// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using System.Diagnostics;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirPathRuleSetDeIdOperation : IDeIdOperation<StringBatchData, StringBatchData>
    {
        private readonly AnonymizerEngine _anonymizerEngine;
        private readonly ILogger<FhirPathRuleSetDeIdOperation> _logger;

        public FhirPathRuleSetDeIdOperation(
            AnonymizerEngine anonymizerEngine, 
            ILogger<FhirPathRuleSetDeIdOperation> logger)
        {
            _anonymizerEngine = EnsureArg.IsNotNull(anonymizerEngine, nameof(anonymizerEngine));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public StringBatchData Process(StringBatchData source)
        {
            var result = new StringBatchData();
            int dataSize = 0;
            foreach (var item in source.Resources)
            {
                dataSize += item.Length * sizeof(char);
                result.Resources.Add(ProcessSingle(item));
            }

            _logger.LogInformation($"[Library] count: {source.Resources.Count} bytes: {dataSize}.");
            return result;
        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }
    }
}