using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Api;
using Presidio.Api;
using Presidio.Client;
using Presidio.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Api
{
    public class PresidioApiHandler : ApiHandler
    {
        private string analyzerLanguage;
        private AnalyzerApi analyzer;
        private AnonymizerApi anonymizer;

        public PresidioApiHandler(string presidioAnalyzedLanguage, string presidioAnalyzerUrl, string presidioAnonymizerUrl)
        {
            analyzerLanguage = presidioAnalyzedLanguage;

            var config = new Configuration();
            config.BasePath = presidioAnalyzerUrl;
            analyzer = new AnalyzerApi(config);

            config = new Configuration();
            config.BasePath = presidioAnonymizerUrl;
            anonymizer = new AnonymizerApi(config);
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

    }
}