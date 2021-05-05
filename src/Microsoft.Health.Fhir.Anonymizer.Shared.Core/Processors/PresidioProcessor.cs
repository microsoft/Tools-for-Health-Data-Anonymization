﻿using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class PresidioProcessor: IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<PresidioProcessor>();
        private string presidioAnalyzerUrl;
        private string presidioAnonymizerUrl;

        public PresidioProcessor(string presidioAnalyzerUrl, string presidioAnonymizerUrl)
        {
            this.presidioAnalyzerUrl = presidioAnalyzerUrl;
            this.presidioAnonymizerUrl = presidioAnonymizerUrl;
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            var input = node.Value.ToString();
            node.Value = string.IsNullOrEmpty(input) ? input : PresidioUtility.Anonymize(input, presidioAnalyzerUrl, presidioAnonymizerUrl);
            _logger.LogDebug($"Fhir value '{input}' at '{node.Location}' is anonymized with Presidio to '{node.Value}'.");

            processResult.AddProcessRecord(AnonymizationOperations.Presidio, node);
            return processResult;
        }
    }
}
