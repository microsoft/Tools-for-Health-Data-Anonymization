using System.Collections.Generic;
using Fhir.Anonymizer.Core.UnitTests.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Utility
{
    public class PresidioUtilityTests
    {
        [Fact]
        public void GivenAText_WhenPresidioAnonymizationCalled_ThenAnonymizedTextShouldBeReturned()
        {
            const string text = "Text For Anonymization";
            string analyzerUrl = "localhost";
            string anonymizerUrl = "localhost";
            string analyzerLanguage = "en";

            var anonymizedText = PresidioUtility.Anonymize(text, new PresidioApiHandlerMock(analyzerLanguage, analyzerUrl, anonymizerUrl));
            Assert.Equal("Anonymized Text", anonymizedText);
        }
    }
}
