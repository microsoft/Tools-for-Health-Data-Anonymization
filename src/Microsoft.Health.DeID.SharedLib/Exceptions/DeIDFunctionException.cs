// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.DeID.SharedLib.Exceptions
{
    public class DeIDFunctionException : Exception
    {
        public DeIDFunctionException(DeIDFunctionErrorCode templateManagementErrorCode, string message)
            : base(message)
        {
            TemplateManagementErrorCode = templateManagementErrorCode;
        }

        public DeIDFunctionException(DeIDFunctionErrorCode templateManagementErrorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            TemplateManagementErrorCode = templateManagementErrorCode;
        }

        public DeIDFunctionErrorCode TemplateManagementErrorCode { get; }
    }
}
