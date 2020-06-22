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
        public async Task<ProcessResult> Process(ElementNode node)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            node.Value = (await TextAnalyticUtility.AnonymizeText(new List<string> { node.Value.ToString() })).First();
            processResult.AddProcessRecord(AnonymizationOperations.CryptoHash, node);
            return processResult;
        }
    }
}
