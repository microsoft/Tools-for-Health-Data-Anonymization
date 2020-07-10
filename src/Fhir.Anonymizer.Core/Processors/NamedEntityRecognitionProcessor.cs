using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Utility.NamedEntityRecognition;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public class NamedEntityRecognitionProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer NamedEntityRecognizer { get; set; }

        private NamedEntityRecognitionProcessor(NamedEntityRecognitionMethod namedEntityRecognitionMethod, string namedEntityRecognitionApiEndpoint, string namedEntityRecognitionApiKey)
        {
            NamedEntityRecognizer = namedEntityRecognitionMethod switch
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

            node.Value = (await NamedEntityRecognizer.AnonymizeText(new List<string> { HttpUtility.HtmlDecode(node.Value.ToString()) })).First();
            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return processResult;
        }
    }
}
