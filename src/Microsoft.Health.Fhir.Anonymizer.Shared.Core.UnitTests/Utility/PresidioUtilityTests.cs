using System.Collections.Generic;
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

            var anonymizedText = PresidioUtility.Anonymize(text);
            
            Assert.Equal("Anonymized Text", anonymizedText);
        }
    }
}
