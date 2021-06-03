// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DeID.SharedLib.Settings
{
    public class EncryptionSetting
    {
        public byte[] AesKey { get; set; }

        public byte[] PublicKey { get; set; }

        public byte[] PrivateKey { get; set; }
    }
}
