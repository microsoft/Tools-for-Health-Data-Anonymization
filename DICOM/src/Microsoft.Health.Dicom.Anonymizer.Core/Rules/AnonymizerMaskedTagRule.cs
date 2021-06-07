﻿// -------------------------------------------------------------------------------------------------
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
    public class AnonymizerMaskedTagRule : AnonymizerRule
    {
        public AnonymizerMaskedTagRule(DicomMaskedTag maskedTag, string method, JObject ruleSetting, string description, IAnonymizerProcessorFactory processorFactory = null, IAnonymizerSettingsFactory settingsFactory = null)
            : base(method, description, ruleSetting, processorFactory, settingsFactory)
        {
            EnsureArg.IsNotNull(maskedTag, nameof(maskedTag));

            MaskedTag = maskedTag;
        }

        public DicomMaskedTag MaskedTag { get; set; }

        public override List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(context, nameof(context));

            var locatedItems = new List<DicomItem>();
            foreach (var item in dataset)
            {
                if (MaskedTag.IsMatch(item.Tag))
                {
                    locatedItems.Add(item);
                }
            }

            return locatedItems.Where(x => !context.VisitedNodes.Contains(x.Tag.ToString())).ToList();
        }
    }
}
