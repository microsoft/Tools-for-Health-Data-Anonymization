// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public class AnonymizerOperationException : DicomAnonymizationException
    {
        public AnonymizerOperationException(DicomAnonymizationErrorCode dicomAnonymizerErrorCode, string message)
            : base(dicomAnonymizerErrorCode, message)
        {
        }

        public AnonymizerOperationException(DicomAnonymizationErrorCode dicomAnonymizerErrorCode, string message, Exception innerException)
            : base(dicomAnonymizerErrorCode, message, innerException)
        {
        }
    }
}
