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
        public NamedEntityRecognitionMethod NamedEntityRecognitionMethod { get; set; }
        
        public string NamedEntityRecognitionApiEndpoint { get; set; } = string.Empty;

        public string NamedEntityRecognitionApiKey { get; set; } = string.Empty;

        public NamedEntityRecognitionProcessor(NamedEntityRecognitionMethod namedEntityRecognitionMethod, string namedEntityRecognitionApiEndpoint, string namedEntityRecognitionApiKey)
        {
            this.NamedEntityRecognitionMethod = namedEntityRecognitionMethod;
            this.NamedEntityRecognitionApiEndpoint = namedEntityRecognitionApiEndpoint;
            this.NamedEntityRecognitionApiKey = namedEntityRecognitionApiKey;
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

            switch (NamedEntityRecognitionMethod)
            {
                case NamedEntityRecognitionMethod.TextAnalytics:
                    node.Value = (await TextAnalyticUtility.AnonymizeText(new List<string> { node.Value.ToString() }, NamedEntityRecognitionApiEndpoint, NamedEntityRecognitionApiKey)).First();
                    break;
                case NamedEntityRecognitionMethod.DeepPavlov:
                    node.Value = (await DeepPavlovUtility.AnonymizeText(new List<string> { node.Value.ToString() }, NamedEntityRecognitionApiEndpoint)).First();
                    break;
                default:
                    throw new NotImplementedException($"The named entity recognition method is not supported: {NamedEntityRecognitionMethod}");
            }
            
            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return processResult;
        }
    }
}
