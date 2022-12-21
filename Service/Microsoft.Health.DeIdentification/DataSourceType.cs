using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Contract
{
    /// <summary>
    /// Supported data source
    /// </summary>
    public enum DataSourceType
    {
        [JsonProperty("fhir")]
        Fhir,

        [JsonProperty("dicom")]
        Dicom,
        /* TreeText, */
        /* Image, */
    }
}
