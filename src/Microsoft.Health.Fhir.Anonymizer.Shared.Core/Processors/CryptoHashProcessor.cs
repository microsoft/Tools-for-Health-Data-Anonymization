using System;
using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
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

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            var descendantsAndSelf = node.DescendantsAndSelf();

            foreach (var element in descendantsAndSelf)
            {
                if (element.Value == null || context?.VisitedNodes != null && context.VisitedNodes.Contains(element))
                {
                    continue;
                }

                var elementNode = (ElementNode) element;
                var input = elementNode.Value.ToString();

                // Hash the id part for "Reference.reference" node and hash whole input for other node types
                if (elementNode.IsReferenceStringNode())
                {
                    var newReference = ReferenceUtility.TransformReferenceId(input, _cryptoHashFunction);
                    elementNode.Value = newReference;
                }
                else
                {
                    elementNode.Value = _cryptoHashFunction(input);
                }

                processResult.AddProcessRecord(AnonymizationOperations.CryptoHash, elementNode);

                _logger.LogDebug($"Fhir value '{input}' at '{elementNode.Location}' is hashed to '{elementNode.Value}'.");
            }

            return processResult;
        }

    }
}
