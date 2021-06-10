// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerTagRule : AnonymizerRule
    {
        public AnonymizerTagRule(DicomTag tag, string method, string description, IAnonymizerProcessorFactory processorFactory, JObject ruleSetting = null)
            : base(method, description, processorFactory, ruleSetting)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));

            Tag = tag;
        }

        public DicomTag Tag { get; set; }

        public override List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(context, nameof(context));

            var item = dataset.GetDicomItem<DicomItem>(Tag);
            var result = new List<DicomItem>();
            if (item != null && !context.VisitedNodes.Contains(item))
            {
                 result.Add(item);
            }

            return result;
        }
    }
}
