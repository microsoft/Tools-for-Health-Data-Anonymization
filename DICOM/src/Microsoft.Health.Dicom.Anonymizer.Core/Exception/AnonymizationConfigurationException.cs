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
        public AnonymizationConfigurationException(DicomAnonymizationErrorCode dicomAnonymizerErrorCode, string message)
            : base(dicomAnonymizerErrorCode, message)
        {
        }

        public AnonymizationConfigurationException(DicomAnonymizationErrorCode dicomAnonymizerErrorCode, string message, Exception innerException)
            : base(dicomAnonymizerErrorCode, message, innerException)
        {
        }
    }
}
