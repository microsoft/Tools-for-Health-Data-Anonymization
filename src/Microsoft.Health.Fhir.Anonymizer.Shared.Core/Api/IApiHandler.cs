using System.Collections.Generic;
using Presidio.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Api
{
    public interface IApiHandler
    {
        List<RecognizerResult> Analyze(string text);
        AnonymizeResponse Anonymize(string text, List<RecognizerResult> analyzerResult);
    }
}