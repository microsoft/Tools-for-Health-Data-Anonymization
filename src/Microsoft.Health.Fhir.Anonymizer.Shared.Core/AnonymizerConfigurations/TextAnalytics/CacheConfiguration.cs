using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class CacheConfiguration
    {
        [DataMember(Name = "enable")]
        public bool Enable { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }
    }
}
