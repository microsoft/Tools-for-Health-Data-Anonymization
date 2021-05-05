using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class PresidioUtility
    {
        public static string Anonymize(string text, string presidioAnalyzerUrl, string presidioAnonymizerUrl)
        {
            var analyzerResult = Analyze(text, presidioAnalyzerUrl);
            var anonymizerResponse = Anonymize(text, presidioAnonymizerUrl, analyzerResult);
            return anonymizerResponse.Text;
        }


        private static List<AnalyzerResult> Analyze(string text, string presidioAnalyzerUrl)
        {
            // Call Analyzer REST API
            return new List<AnalyzerResult>(new[] {new AnalyzerResult()});
        }
        
        private static AnonymizeResponse Anonymize(string text, string presidioAnonymizerUrl, List<AnalyzerResult> analyzerResult)
        {
            // Call Anonymizer REST API
            var anonymizeResponse = new AnonymizeResponse();
            anonymizeResponse.Text = "Anonymized Text";
            return anonymizeResponse;
        }
        

    }
    
    //TODO: [ADO-3166] Stub, replace with the actual SDK object.
    public class AnalyzerResult
    {
    }

    //TODO: [ADO-3166] Stub, replace with the actual SDK object.
    public class AnonymizeResponse
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        
    }


}
