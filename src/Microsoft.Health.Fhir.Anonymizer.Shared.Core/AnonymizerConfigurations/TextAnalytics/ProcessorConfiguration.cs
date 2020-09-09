using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class ProcessorConfiguration
    {
        [DataMember(Name = "recognizer")]
        public RecognizerConfiguration recognizerConfiguration { get; set; }

        [DataMember(Name = "multithreading")]
        public MultithreadingConfiguration Multithreading { get; set; }
    }
}
