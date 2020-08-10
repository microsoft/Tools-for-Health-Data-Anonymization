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
        [InlineData("1+1")]
        [InlineData("1_1")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenAnInvalidId_WhenValidateAResource_ThenValidationErrorsShouldBeReturned(string id)
        {
            var resource = new Patient
            {
                Id = id
            };

            var validationErrors = _validator.Validate(resource).ToList();
            Assert.Single(validationErrors);

            var expectedError = id + " is not a correctly formatted Id";
            var actualError = validationErrors.FirstOrDefault()?.ErrorMessage;
            Assert.Equal(expectedError, actualError);
        }

        [Theory]
        [InlineData("******")]
        [InlineData("Should not be valid")]
        [InlineData("<body>Should not be valid</body>")]
        [InlineData("<div xmlns='http://www.w3.org/1999/xhtml'><p>should not be valid<p></div>")]
        public void GivenAnInvalidNarrative_WhenValidateAResource_ThenValidationErrorsShouldBeReturned(string div)
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

            var expectedError = "Xml can not be parsed or is not valid according to the (limited) FHIR scheme";
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

            var expectedError = "Element with min. cardinality 1 cannot be null";
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
