// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class KeepProcessor : IAnonymizerProcessor
    {
        public void Process(DicomDataset dicomDataset, DicomItem item, DicomBasicInformation basicInfo = null, IDicomAnonymizationSetting settings = null)
        {
        }
    }
}
