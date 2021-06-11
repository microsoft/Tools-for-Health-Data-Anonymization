// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public class DicomAnonymizationException : Exception
    {
        public DicomAnonymizationException(DicomAnonymizationErrorCode dicomAnonymizerErrorCode, string message)
            : base(message)
        {
            DicomAnonymizerErrorCode = dicomAnonymizerErrorCode;
        }

        public DicomAnonymizationException(DicomAnonymizationErrorCode dicomAnonymizerErrorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            DicomAnonymizerErrorCode = dicomAnonymizerErrorCode;
        }

        public DicomAnonymizationErrorCode DicomAnonymizerErrorCode { get; }
    }
}
