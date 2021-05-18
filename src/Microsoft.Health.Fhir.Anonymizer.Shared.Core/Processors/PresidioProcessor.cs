﻿using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Api;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class PresidioProcessor: IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<PresidioProcessor>();
        private IApiHandler presidioApiHandler;

        public PresidioProcessor(IApiHandler presidioApiHandler)
        {
            this.presidioApiHandler = presidioApiHandler;
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            var input = node.Value.ToString();
            node.Value = string.IsNullOrEmpty(input) ? input : PresidioUtility.Anonymize(input, presidioApiHandler);
            _logger.LogDebug($"Fhir value '{input}' at '{node.Location}' is anonymized with Presidio to '{node.Value}'.");

            processResult.AddProcessRecord(AnonymizationOperations.Presidio, node);
            return processResult;
        }
    }
}
