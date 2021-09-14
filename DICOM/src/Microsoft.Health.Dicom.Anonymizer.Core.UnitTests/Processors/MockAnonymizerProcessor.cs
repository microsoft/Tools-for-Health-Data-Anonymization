// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.Processors
{
    public class MockAnonymizerProcessor : IAnonymizerProcessor
    {
        public bool IsSupported(DicomItem item)
        {
            throw new NotImplementedException();
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
        {
            throw new NotImplementedException();
        }
    }
}
