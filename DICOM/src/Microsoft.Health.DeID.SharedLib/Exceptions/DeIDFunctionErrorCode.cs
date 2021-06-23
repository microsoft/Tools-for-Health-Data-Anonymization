// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.DeID.SharedLib.Exceptions
{
    public enum DeIDFunctionErrorCode
    {
        InvalidInputValue,
        InvalidDeIdSettings,

        DateShiftFailed,
        EncryptFailed,
        CryptoHashFailed,
        PerturbFailed,
        RedactFailed,
    }
}
