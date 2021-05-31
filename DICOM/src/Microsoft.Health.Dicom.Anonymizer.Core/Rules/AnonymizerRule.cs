// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerRule
    {
        public AnonymizerRule(string method, string description, IDicomAnonymizationSetting ruleSetting, IAnonymizerProcessorFactory processorFactory)
        {
            Description = description;
            Processor = processorFactory.CreateProcessor(method, ruleSetting);
        }

        public string Description { get; set; }

        public IAnonymizerProcessor Processor { get; set; }

        public void Handle(DicomDataset dataset, ProcessContext context)
        {
            var locatedItems = LocateDicomTag(dataset, context);

            foreach (var item in locatedItems)
            {
                Processor.Process(dataset, item, context);
                context.VisitedNodes.Add(item.Tag.ToString());
            }
        }

        public virtual List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
        {
            return null;
        }
    }
}
