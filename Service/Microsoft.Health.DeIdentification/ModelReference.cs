using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Contract
{
    [DataContract]
    public class ModelReference
    {
        [DataMember(Name = "modelType")]
        public DeidModelType ModelType { get; set; }

        [DataMember(Name = "configurationLocation")]
        public string ConfigurationLocation { get; set; }

    }
}
