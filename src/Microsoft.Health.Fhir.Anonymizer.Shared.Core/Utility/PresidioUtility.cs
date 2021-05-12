
namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class PresidioUtility

    {        
        public static string Anonymize(string text, IApiHandler apiHandler)
        {
            var analyzerResult = apiHandler.Analyze(text);
            var anonymizerResponse = apiHandler.Anonymize(text, analyzerResult);
            return anonymizerResponse.Text;           
        }        
    }   
}
