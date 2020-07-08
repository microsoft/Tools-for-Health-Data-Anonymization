using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility.NamedEntityRecognition;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public class NamedEntityRecognitionProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer _namedEntityRecognizer { get; set; }

        public NamedEntityRecognitionProcessor(NamedEntityRecognitionMethod namedEntityRecognitionMethod, string namedEntityRecognitionApiEndpoint, string namedEntityRecognitionApiKey)
        {
            _namedEntityRecognizer = namedEntityRecognitionMethod switch
            {
                NamedEntityRecognitionMethod.TextAnalytics => new TextAnalyticRecognizer(namedEntityRecognitionApiEndpoint, namedEntityRecognitionApiKey),
                NamedEntityRecognitionMethod.DeepPavlov => new DeepPavlovRecognizer(namedEntityRecognitionApiEndpoint),
                _ => throw new NotImplementedException($"The named entity recognition method is not supported: {namedEntityRecognitionMethod}"),
            };
        }

        public static NamedEntityRecognitionProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new NamedEntityRecognitionProcessor(parameters.NamedEntityRecognitionMethod, parameters.NamedEntityRecognitionApiEndpoint, parameters.NamedEntityRecognitionApiKey);
        }

        public async Task<ProcessResult> Process(ElementNode node)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            node.Value = (await _namedEntityRecognizer.AnonymizeText(new List<string> { node.Value.ToString() })).First();
            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return processResult;
        }
    }
}
