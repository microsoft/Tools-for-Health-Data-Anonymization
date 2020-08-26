using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    public static class SecurityLabels
    {
        public static readonly Coding REDACT = new Coding()
        {
            System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            Code = "REDACTED",
            Display = "part of the resource is redacted"
        };

        public static readonly Coding ABSTRED = new Coding()
        {
            System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            Code = "ABSTRED",
            Display = "exact value is replaced with a range"
        };

        public static readonly Coding CRYTOHASH = new Coding()
        {
            System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            Code = "CRYTOHASH",
            Display = "exact value is transformed with a hash function"
        };

        public static readonly Coding ENCRYPT = new Coding()
        {
            System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            Code = "ENCRYPT",
            Display = "exact value is transformed into ciphertext"
        };

        public static readonly Coding PERTURBED = new Coding()
        {
            Code = "PERTURBED",
            Display = "exact value is replaced with another exact value"
        };

        public static readonly Coding SUBSTITUTED = new Coding()
        {
            Code = "SUBSTITUTED",
            Display = "exact value is replaced with a predefined value"
        };
    }
}
