// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Model
{
    public class ProcessContext
    {
        public HashSet<DicomItem> VisitedNodes { get; set; } = new HashSet<DicomItem>();

        public string StudyInstanceUID { get; set; }

        public string SeriesInstanceUID { get; set; }

        public string SopInstanceUID { get; set; }
    }
}
