using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Presidio.Model;

namespace Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class PresidioApiHandlerMock : IApiHandler
    {
        public PresidioApiHandlerMock(string presidioAnalyzedLanguage, string presidioAnalyzerUrl, string presidioAnonymizerUrl)
        {            
        }

        public List<RecognizerResult> Analyze(string text)
        {
            return new List<RecognizerResult>();
        }

        public AnonymizeResponse Anonymize(string text, List<RecognizerResult> analyzerResult)
        {
            var anonymizeResponse = new AnonymizeResponse();
            anonymizeResponse.Text = "Anonymized Text";
            return anonymizeResponse;
        }
    }
}
