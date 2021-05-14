// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public class DicomAnonymizationException : Exception
    {
        public DicomAnonymizationException(DicomAnonymizationErrorCode templateManagementErrorCode, string message)
            : base(message)
        {
            TemplateManagementErrorCode = templateManagementErrorCode;
        }

        public DicomAnonymizationException(DicomAnonymizationErrorCode templateManagementErrorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            TemplateManagementErrorCode = templateManagementErrorCode;
        }

        public DicomAnonymizationErrorCode TemplateManagementErrorCode { get; }
    }
}
