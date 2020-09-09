using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class MappingConfiguration
    {
        [DataMember(Name = "LOCATION")]
        public List<BaseCategory> Location { get; set; }

        [DataMember(Name = "CONTACT")]
        public List<BaseCategory> Contact { get; set; }

        [DataMember(Name = "NAME")]
        public List<BaseCategory> Name { get; set; }

        [DataMember(Name = "PROFESSION")]
        public List<BaseCategory> Profession { get; set; }

        [DataMember(Name = "AGE")]
        public List<BaseCategory> Age { get; set; }

        [DataMember(Name = "DATE")]
        public List<BaseCategory> Date { get; set; }

        [DataMember(Name = "ID")]
        public List<BaseCategory> Id { get; set; }
    }
}
