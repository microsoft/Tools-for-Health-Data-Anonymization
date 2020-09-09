using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class ApiConfiguration
    {
        [DataMember(Name = "endpoint")]
        public string Endpoint { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "key")]
        public string Key { get; set; }

        // keyId is like a public key to processor (used in AmazonProcessor), while the key is a private key
        [DataMember(Name = "keyId")]
        public string KeyId { get; set; }
    }
}
