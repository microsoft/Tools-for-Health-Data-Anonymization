using System.Runtime.Serialization;

namespace Microsoft.Health.DeIdentification.Contract
{
    [DataContract]
    public class DeIdConfigurationSection
    {
        [DataMember(Name = "deIdConfigurations")]
        public DeIdConfiguration[] DeIdConfigurations { get; set; }
    }
}
