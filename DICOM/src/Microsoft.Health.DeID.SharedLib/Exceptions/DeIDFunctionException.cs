// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.DeID.SharedLib.Exceptions
{
    public class DeIDFunctionException : Exception
    {
        public DeIDFunctionException(DeIDFunctionErrorCode deIdentificationErrorCode, string message)
            : base(message)
        {
            DeIdentificationErrorCode = deIdentificationErrorCode;
        }

        public DeIDFunctionException(DeIDFunctionErrorCode deIdentificationErrorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            DeIdentificationErrorCode = deIdentificationErrorCode;
        }

        public DeIDFunctionErrorCode DeIdentificationErrorCode { get; }
    }
}
