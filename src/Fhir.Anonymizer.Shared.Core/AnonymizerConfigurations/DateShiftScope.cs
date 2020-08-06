using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MicrosoftFhir.Anonymizer.Core.AnonymizerConfigurations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DateShiftScope
    {
        [EnumMember(Value = "resource")]
        Resource,
        [EnumMember(Value = "file")]
        File,
        [EnumMember(Value = "folder")]
        Folder
    }
}
