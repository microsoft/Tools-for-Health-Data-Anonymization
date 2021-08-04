using System.Collections.Generic;
using System.Text;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class EncryptProcessor: IAnonymizerProcessor
    {
        private readonly byte[] _key;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<EncryptProcessor>();

        public EncryptProcessor(string encryptKey)
        {
            _key = Encoding.UTF8.GetBytes(encryptKey);
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
                elementNode.Value = EncryptUtility.EncryptTextToBase64WithAes(input, _key);
                processResult.AddProcessRecord(AnonymizationOperations.Encrypt, elementNode);

                _logger.LogDebug($"Fhir value '{input}' at '{elementNode.Location}' is encrypted to '{elementNode.Value}'.");
            }

            return processResult;
        }
    }
}
