using System;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core.Processors
{
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
        private readonly string _cryptoHashKey;
        private readonly Func<string, string> _cryptoHashFunction; 
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<CryptoHashProcessor>();

        public CryptoHashProcessor(string cryptoHashKey)
        {
            _cryptoHashKey = cryptoHashKey;
            _cryptoHashFunction = (input) => CryptoHashUtility.ComputeHmacSHA256Hash(input, _cryptoHashKey);
        }

        public async Task<ProcessResult> Process(ElementNode node)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            var input = node.Value.ToString();
            // Hash the id part for "Reference.reference" node and hash whole input for other node types
            if (node.IsReferenceStringNode())
            {
                var newReference = ReferenceUtility.TransformReferenceId(input, _cryptoHashFunction);
                node.Value = newReference;
            }
            else
            {
                node.Value = _cryptoHashFunction(input);
            }

            _logger.LogDebug($"Fhir value '{input}' at '{node.Location}' is hashed to '{node.Value}'.");

            processResult.AddProcessRecord(AnonymizationOperations.CryptoHash, node);
            return processResult;
        }

    }
}
