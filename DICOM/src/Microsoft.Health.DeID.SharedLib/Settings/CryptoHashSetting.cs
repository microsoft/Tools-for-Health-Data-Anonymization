// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Security.Authentication;

namespace Microsoft.Health.DeID.SharedLib.Settings
{
    public class CryptoHashSetting
    {
        public string CryptoHashKey { get; set; }

        public HashAlgorithmType CryptoHashType { get; set; } = HashAlgorithmType.Sha256;
    }
}
