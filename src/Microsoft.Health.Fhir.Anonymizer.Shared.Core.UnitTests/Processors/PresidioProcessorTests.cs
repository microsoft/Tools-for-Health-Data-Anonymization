using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;
namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class PresidioProcessorTests
    {

        [Fact]
        public void GivenANode_WhenProcessingWithPresidio_ValueShouldBeAnonymizedWithPresidio()
        {
            string analyzerUrl = "localhost";
            string anonymizerUrl = "localhost";
            var element = ElementNode.FromElement(new FhirString("Text For Anonymization").ToTypedElement()); 
            var processor = new PresidioProcessor(analyzerUrl, anonymizerUrl);

            var processResult = processor.Process(element);

            Assert.Equal("Anonymized Text", element.Value.ToString());
            Assert.True(processResult.IsPresidioAnonymized);
        }

    }
}
