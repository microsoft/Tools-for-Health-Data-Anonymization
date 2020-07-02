using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core.Processors
{
    public class TextAnalyticProcessor : IAnonymizerProcessor
    {
        public string TextAnalyticApiEndpoint { get; set; } = string.Empty;

        public string TextAnalyticApiKey { get; set; } = string.Empty;

        public TextAnalyticProcessor(string textAnalyticApiEndpoint, string textAnalyticApiKey)
        {
            this.TextAnalyticApiEndpoint = textAnalyticApiEndpoint;
            this.TextAnalyticApiKey = textAnalyticApiKey;
        }

        public static TextAnalyticProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new TextAnalyticProcessor(parameters.TextAnalyticApiEndpoint, parameters.TextAnalyticApiKey);
        }

        public async Task<ProcessResult> Process(ElementNode node)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            node.Value = (await TextAnalyticUtility.AnonymizeText(new List<string> { node.Value.ToString() }, TextAnalyticApiEndpoint, TextAnalyticApiKey)).First();
            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return processResult;
        }
    }
}
