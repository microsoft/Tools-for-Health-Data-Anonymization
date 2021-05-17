// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    internal static class Constants
    {
        internal const string TagKey = "tag";
        internal const string VRKey = "VR";
        internal const string MethodKey = "method";
        internal const string Parameters = "params";
        internal const string RuleSetting = "setting";

        internal const string RedactDefaultSetting = "redactDefaultSetting";
        internal const string PerturbDefaultSetting = "perturbDefaultSetting";
        internal const string EncryptDefaultSetting = "encryptDefaultSetting";
        internal const string CryptoHashDefaultSetting = "cryptoHashDefaultSetting";
        internal const string DateShiftDefaultSetting = "dateShiftDefaultSetting";
        internal const string SubstituteDefaultSetting = "substituteDefaultSetting";
    }
}
