// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerTagRule : AnonymizerRule
    {
        public AnonymizerTagRule(DicomTag tag, string method, IDicomAnonymizationSetting ruleSetting, string description, IAnonymizerProcessorFactory processorFactory)
            : base(method, description, ruleSetting, processorFactory)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));

            Tag = tag;
        }

        public DicomTag Tag { get; set; }

        public override List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
        {
            var locatedItems = new List<DicomItem>() { dataset.GetDicomItem<DicomItem>(Tag) };
            return locatedItems.Where(x => x != null && !context.VisitedNodes.Contains(x.Tag.ToString())).ToList();
        }
    }
}
