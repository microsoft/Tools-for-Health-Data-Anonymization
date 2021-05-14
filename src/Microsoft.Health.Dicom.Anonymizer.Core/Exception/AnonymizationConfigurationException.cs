// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public class AnonymizationConfigurationException : DicomAnonymizationException
    {
        public AnonymizationConfigurationException(DicomAnonymizationErrorCode templateManagementErrorCode, string message)
            : base(templateManagementErrorCode, message)
        {
        }

        public AnonymizationConfigurationException(DicomAnonymizationErrorCode templateManagementErrorCode, string message, Exception innerException)
            : base(templateManagementErrorCode, message, innerException)
        {
        }
    }
}
