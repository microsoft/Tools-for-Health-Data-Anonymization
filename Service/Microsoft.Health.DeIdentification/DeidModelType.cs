using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Contract
{
    public enum DeidModelType
    {
        [JsonProperty("fhirR4PathRuleSet")]
        FhirR4PathRuleSet,

        [JsonProperty("fhirStu3PathRuleSet")]
        FhirStu3PathRuleSet,

        [JsonProperty("dicomMetadataRuleSet")]
        DicomMetadataRuleSet,

        [JsonProperty("freeTextFakeModel")]
        FreeTextFakeModel,

        [JsonProperty("imageFakeModel")]
        ImageFakeModel,
    }
}
