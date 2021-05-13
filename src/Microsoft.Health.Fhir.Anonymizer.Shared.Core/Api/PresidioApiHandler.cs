using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Api;
using Presidio.Api;
using Presidio.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Api
{
    public class PresidioApiHandler : IApiHandler
    {
        private string analyzerLanguage;
        private IAnalyzerApi analyzer;
        private IAnonymizerApi anonymizer;

        public PresidioApiHandler(string analyzerLanguage, IAnalyzerApi analyzer, IAnonymizerApi anonymizer)
        {
            this.analyzerLanguage = analyzerLanguage;
            this.analyzer = analyzer;
            this.anonymizer = anonymizer;
        }

        public List<RecognizerResult> Analyze(string text)
        {
            var analyzerRequest = new AnalyzeRequest(text, analyzerLanguage);
            var result = analyzer.AnalyzePost(analyzerRequest);
            return result.Select(r => new RecognizerResult(r.Start, r.End, r.Score, r.EntityType)).ToList();
        }

        public AnonymizeResponse Anonymize(string text, List<RecognizerResult> analyzerResult)
        {
            AnonymizeRequest anonymizerRequest = new AnonymizeRequest(text, analyzerResults: analyzerResult);
            return anonymizer.AnonymizePost(anonymizerRequest);
        }

        public static PresidioApiHandler Instantiate(ParameterConfiguration parameterConfiguration)
        {
            return
                new PresidioApiHandler(parameterConfiguration.PresidioAnalyzedLanguage,
                    new AnalyzerApi(parameterConfiguration.PresidioAnalyzerUrl),
                    new AnonymizerApi(parameterConfiguration.PresidioAnonymizerUrl));
        }

    }
}