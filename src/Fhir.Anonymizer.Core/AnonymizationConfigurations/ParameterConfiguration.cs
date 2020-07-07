using System.Collections.Generic;
using System.Runtime.Serialization;
using Fhir.Anonymizer.Core.AnonymizationConfigurations;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class ParameterConfiguration
    {
        [DataMember(Name = "dateShiftKey")]
        public string DateShiftKey { get; set; }

        [DataMember(Name = "dateShiftScope")]
        public DateShiftScope DateShiftScope { get; set; }

        [DataMember(Name = "cryptoHashKey")]
        public string CryptoHashKey { get; set; }

        [DataMember(Name = "namedEntityRecognitionMethod")]
        public NamedEntityRecognitionMethod NamedEntityRecognitionMethod { get; set; }
        
        [DataMember(Name = "namedEntityRecognitionApiEndpoint")]
        public string NamedEntityRecognitionApiEndpoint { get; set; }

        [DataMember(Name = "namedEntityRecognitionApiKey")]
        public string NamedEntityRecognitionApiKey { get; set; }

        [DataMember(Name = "enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; }

        [DataMember(Name = "enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; }

        [DataMember(Name = "enablePartialZipCodesForRedact")]
        public bool EnablePartialZipCodesForRedact { get; set; }

        [DataMember(Name = "restrictedZipCodeTabulationAreas")]
        public List<string> RestrictedZipCodeTabulationAreas { get; set; }

        public string DateShiftKeyPrefix { get; set; }
    }
}
