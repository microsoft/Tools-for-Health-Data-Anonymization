using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Contract
{
    [DataContract]
    public class Configuration
    {
        [DataMember(Name = "deidConfigurations")]
        public DeIdConfiguration[] DeidConfigurations { get; set; }
    }
}
