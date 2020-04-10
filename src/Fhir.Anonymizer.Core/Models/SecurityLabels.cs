using Hl7.Fhir.Model;

namespace Fhir.Anonymizer.Core.Models
{
    public static class SecurityLabels
    {
        public static readonly Coding REDACT = new Coding()
        {
            System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            Code = "REDACTED",
            Display = "part of the resource is removed"
        };

        public static readonly Coding ABSTRED = new Coding()
        {
            System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            Code = "ABSTRED",
            Display = "exact value is replaced with a range"
        };

        public static readonly Coding PERTURBED = new Coding()
        {
            Code = "PERTURBED",
            Display = "exact value is replaced with another exact value"
        };
    }
}
