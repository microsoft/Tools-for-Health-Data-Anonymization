using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class MultithreadingConfiguration
    {
        [DataMember(Name = "enable")]
        public bool Enable { get; set; }

        [DataMember(Name = "threads")]
        public int Threads { get; set; }
    }
}
