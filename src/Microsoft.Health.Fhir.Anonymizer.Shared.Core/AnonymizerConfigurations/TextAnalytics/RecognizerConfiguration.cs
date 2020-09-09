using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class RecognizerConfiguration
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "type")]
        public RecognizerType Type { get; set; }

        [DataMember(Name = "api")]
        public ApiConfiguration Api { get; set; }

        [DataMember(Name = "cache")]
        public CacheConfiguration Cache { get; set; }

        [DataMember(Name = "mapping")]
        public MappingConfiguration Mapper { get; set; }
    }
}
