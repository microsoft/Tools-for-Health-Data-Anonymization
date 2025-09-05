﻿using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Validation
{
    public class AttributeValidatorTests
    {
        private readonly FhirJsonParser _parser = new FhirJsonParser();
        private readonly AttributeValidator _validator = new AttributeValidator();

        [Theory]
        [InlineData("******", "is not a correct literal for an id")]
        [InlineData("Should not be valid", "is not a correct literal for an id")]
        [InlineData("<body>Should not be valid</body>", "is not a correct literal for an id")]
        [InlineData("<div xmlns='http://www.w3.org/1999/xhtml'><p>should not be valid<p></div>", "is not a correct literal for an id")]
        public void GivenAnInvalidId_WhenValidateAResource_ThenValidationErrorsShouldBeReturned(string id, string expectedError)
        {
            var resource = new Patient
            {
                Id = id
            };

            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);

            var actualError = validationErrors.FirstOrDefault()?.ErrorMessage;
            Assert.Contains(expectedError, actualError);
        }

        [Theory]
        [InlineData("******", "Value is not well-formatted Xml")]
        [InlineData("Should not be valid", "Value is not well-formatted Xml: Invalid Xml encountered.")]
        [InlineData("<body>Should not be valid</body>", "Value is not well-formed Xml adhering to the FHIR schema for Narrative")]
        [InlineData("<div xmlns='http://www.w3.org/1999/xhtml'><p>should not be valid<p></div>", "Value is not well-formatted Xml")]
        public void GivenAnInvalidNarrative_WhenValidateAResource_ThenValidationErrorsShouldBeReturned(string div, string expectedError)
        {
            var resource = new Patient
            {
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = div
                }
            };

            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);

            var actualError = validationErrors.FirstOrDefault()?.ErrorMessage;
            Assert.Contains(expectedError, actualError);
        }
        
        [Fact]
        public void GivenAMissingAttribute_WhenValidateAResource_ThenValidationErrorsShouldBeReturned()
        {
            // Given a Task with intent field missing
            var resource = new Hl7.Fhir.Model.Task()
            {
                Status = Hl7.Fhir.Model.Task.TaskStatus.Accepted
            };

            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);

            var expectedError = "Element 'IntentElement' with minimum cardinality 1 cannot be null. At Task.IntentElement, line , position ";
            var actualError = validationErrors.FirstOrDefault()?.ErrorMessage;
            Assert.Equal(expectedError, actualError);

            var expectedPath = "IntentElement";
            var actualPath = validationErrors.FirstOrDefault()?.MemberNames?.FirstOrDefault();
            Assert.Equal(expectedPath, actualPath);
        }
        
        [Fact]
        public void GivenAnInvalidBundleEntry_WhenValidateAResource_ThenValidationErrorsShouldBeReturned()
        {
            var bundle = _parser.Parse<Bundle>(File.ReadAllText("./TestResources/bundle-basic.json"));
            var validationErrors = _validator.Validate(bundle).ToList();
            Assert.Empty(validationErrors);

            var observationEntry = bundle.FindEntry("http://example.org/fhir/Observation/123").FirstOrDefault().Resource as Observation;
            observationEntry.Status = null;
            validationErrors = _validator.Validate(bundle).ToList();
            Assert.Single(validationErrors);
        }

        [Fact]
        public void GivenAnInvalidContainedResource_WhenValidateAResource_ThenValidationErrorsShouldBeReturned()
        {
            var resource = _parser.Parse<Condition>(File.ReadAllText("./TestResources/contained-basic.json"));
            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Empty(validationErrors);

            var contained = resource.Contained.FirstOrDefault();
            contained.Id = "******";
            validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);
        }
    }
}
