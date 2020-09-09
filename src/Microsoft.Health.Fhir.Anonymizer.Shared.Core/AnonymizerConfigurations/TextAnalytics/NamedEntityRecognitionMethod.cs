using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
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
