// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
