using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class PresidioUtility
    {
        public static string Anonymize(string text)
        {
            var analyzerResult = Analyze(text);
            var anonymizerResponse = Anonymize(text, analyzerResult);
            return anonymizerResponse.Text;
        }


        private static List<AnalyzerResult> Analyze(string text)
        {
            // Call Analyzer REST API
            return new List<AnalyzerResult>(new[] {new AnalyzerResult()});
        }
        
        private static AnonymizeResponse Anonymize(string text, List<AnalyzerResult> analyzerResult)
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
