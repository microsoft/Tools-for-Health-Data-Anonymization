using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Fhir.Anonymizer.Core.Processors
{
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
        private readonly string _cryptoHashKey;
        private readonly Func<string, string> _cryptoHashFunction;

        public CryptoHashProcessor(string cryptoHashKey)
        {
            _cryptoHashKey = cryptoHashKey;
            _cryptoHashFunction = (input) => CryptoHashUtility.ComputeHmacSHA256Hash(input, _cryptoHashKey);
        }

        public ProcessResult Process(ElementNode node)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            // Hash the id part for "Reference.reference" node and hash whole value for other node types
            if (node.IsReferenceNode())
            {
                var newReference = ReferenceUtility.TransformReferenceId(node.Value.ToString(), _cryptoHashFunction);
                node.Value = newReference;
            }
            else
            {
                node.Value = _cryptoHashFunction(node.Value.ToString());
            }

            processResult.AddProcessRecord(AnonymizationOperations.CryptoHash, node);
            return processResult;
        }

    }
}
