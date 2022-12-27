// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
