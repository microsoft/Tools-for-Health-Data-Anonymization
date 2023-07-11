using System.IO;
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
        [InlineData("1+1", "'1+1' is not a correct literal for an id. At Patient.IdElement.Value.")]
        [InlineData("1_1", "'1_1' is not a correct literal for an id. At Patient.IdElement.Value.")]
        [InlineData("11|", "'11|' is not a correct literal for an id. At Patient.IdElement.Value.")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065", "'00000000000000000000000000000000000000000000000000000000000000065' is not a correct literal for an id. At Patient.IdElement.Value.")]
        public void GivenAnInvalidId_WhenValidateAResource_ThenValidationErrorsShouldBeReturned(string id, string expectedError)
        {
            var resource = new Patient
            {
                Id = id
            };

            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);

            var actualError = validationErrors.FirstOrDefault()?.ErrorMessage;
            Assert.Equal(expectedError, actualError);
        }

        [Theory]
        [InlineData("******", "Value is not well-formatted Xml: Invalid Xml encountered. Details: Data at the root level is invalid. Line 1, position 1. At Patient.Text.Div, line , position ")]
        [InlineData("Should not be valid", "Value is not well-formatted Xml: Invalid Xml encountered. Details: Data at the root level is invalid. Line 1, position 1. At Patient.Text.Div, line , position ")]
        [InlineData("<body>Should not be valid</body>", "Value is not well-formed Xml adhering to the FHIR schema for Narrative: Root element of XHTML is not a <div> from the XHTML namespace (http://www.w3.org/1999/xhtml). At Patient.Text.Div, line , position ")]
        [InlineData("<div xmlns='http://www.w3.org/1999/xhtml'><p>should not be valid<p></div>", "Value is not well-formatted Xml: Invalid Xml encountered. Details: The 'p' start tag on line 1 position 66 does not match the end tag of 'div'. Line 1, position 70. At Patient.Text.Div, line , position ")]
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
            Assert.Equal(expectedError, actualError);
        }
        
        [Fact]
        public void GivenAMissingAttribute_WhenValidateAResource_ThenValidationErrorsShouldBeReturned()
        {
            // Given a Task with intent field missing
            var resource = new Task()
            {
                Status = Task.TaskStatus.Accepted
            };

            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);

            var expectedError = "Element with minimum cardinality 1 cannot be null. At Task.IntentElement.";
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
