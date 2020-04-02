using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core.Validation
{
    public class ResourceValidator
    {
        private readonly AttributeValidator _validator = new AttributeValidator();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<ResourceValidator>();

        public void ValidateInput(Resource resource)
        {
            var result = _validator.Validate(resource);
            foreach (var error in result)
            {
                var path = string.IsNullOrEmpty(error.MemberNames?.FirstOrDefault()) ? string.Empty : error.MemberNames?.FirstOrDefault();                   
                _logger.LogDebug(string.IsNullOrEmpty(resource?.Id) ?
                    $"The input is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}." :
                    $"The input of resource ID {resource.Id} is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}.");
            }
        }

        public void ValidateOutput(Resource resource)
        {
            var result = _validator.Validate(resource);
            foreach (var error in result)
            {
                var path = error.MemberNames?.FirstOrDefault() ?? string.Empty;
                _logger.LogDebug(string.IsNullOrEmpty(resource?.Id) ?
                    $"The output is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}." :
                    $"The output of resource ID {resource.Id} is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}.");
            }
        }
    }
}
