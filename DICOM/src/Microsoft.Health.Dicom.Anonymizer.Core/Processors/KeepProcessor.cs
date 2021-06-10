// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// Keep the original value.
    /// </summary>
    public class KeepProcessor : IAnonymizerProcessor
    {
        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
        }

        public bool IsSupportedVR(DicomItem item)
        {
            return true;
        }
    }
}
