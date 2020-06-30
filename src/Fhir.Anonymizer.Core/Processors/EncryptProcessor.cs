using System.Text;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core.Processors
{
    public class EncryptProcessor: IAnonymizerProcessor
    {
        private readonly byte[] _key;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<EncryptProcessor>();

        public EncryptProcessor(string encryptKey)
        {
            _key = Encoding.UTF8.GetBytes(encryptKey);
        }

        public ProcessResult Process(ElementNode node)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            var input = node.Value.ToString();
            node.Value = EncryptUtility.EncryptTextToBase64WithAes(input, _key);
            _logger.LogDebug($"Fhir value '{input}' at '{node.Location}' is encrypted to '{node.Value}'.");

            processResult.AddProcessRecord(AnonymizationOperations.Encrypt, node);

            return processResult;
        }
    }
}
