using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fhir.Anonymizer.Core.AnonymizationConfigurations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NamedEntityRecognitionMethod
    {
        [EnumMember(Value = "textAnalytics")]
        TextAnalytics,
        [EnumMember(Value = "deepPavlov")]
        DeepPavlov
    }
}
