using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fhir.Anonymizer.Core.Models.DeepPavlov
{
    [DataContract]
    public class DeepPavlovRequestContent
    {
        [DataMember(Name = "x")]
        public IEnumerable<string> X { get; set; }
    }
}
