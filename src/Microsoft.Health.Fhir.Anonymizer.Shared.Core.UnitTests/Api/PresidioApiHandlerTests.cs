using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Api;
using Presidio.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Api
{
    public class PresidioApiHandlerTests
    {
        [Fact]
        public void GivenAnalyzeCalled_WhenAnalyzerApiCalled_PresidioTextAnalysisRecognizerResultsShouldBeReturned()
        {
            var presidioApiHandler = new PresidioApiHandler("en", new AnalyzerApiMock(), new AnonymizerApiMock());

            var recognizerResults = presidioApiHandler.Analyze("Text For Analysis");

            var recognizerResult = recognizerResults[0];
            Assert.Equal(AnalyzerApiMock.DefaultStart, recognizerResult.Start);
            Assert.Equal(AnalyzerApiMock.DefaultEnd, recognizerResult.End);
            Assert.Equal(AnalyzerApiMock.DefaultScore, recognizerResult.Score);
            Assert.Equal(AnalyzerApiMock.EntityType, recognizerResult.EntityType);
        }
        
        [Fact]
        public void GivenAnonymizeCalled_WhenAnonymizerApiCalled_PresidioAnonymizeResponseShouldBeReturned()
        {
            var presidioApiHandler = new PresidioApiHandler("en", new AnalyzerApiMock(), new AnonymizerApiMock());

            var anonymizeResponse = presidioApiHandler.Anonymize("Text For Analysis", new List<RecognizerResult>());

            Assert.Equal(AnonymizerApiMock.DefaultText, anonymizeResponse.Text);
            Assert.Same(AnonymizerApiMock.s_defaultOperatorEntities, anonymizeResponse.Items);
            
        }

    }
}