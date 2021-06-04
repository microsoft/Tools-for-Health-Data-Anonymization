// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public abstract class AnonymizerRule
    {
        public AnonymizerRule(string method, string description, JObject ruleSetting = null, IAnonymizerProcessorFactory processorFactory = null, IDeIDSettingsFactory settingsFactory = null)
        {
            EnsureArg.IsNotNull(method, nameof(method));
            EnsureArg.IsNotNull(description, nameof(description));

            Description = description;
            processorFactory ??= new DicomProcessorFactory();
            Processor = processorFactory.CreateProcessor(method, ruleSetting, settingsFactory);
        }

        public string Description { get; set; }

        public IAnonymizerProcessor Processor { get; set; }

        public void Handle(DicomDataset dataset, ProcessContext context)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(context, nameof(context));

            var locatedItems = LocateDicomTag(dataset, context);

            foreach (var item in locatedItems)
            {
                Processor.Process(dataset, item, context);
                context.VisitedNodes.Add(item.Tag.ToString());
            }
        }

        public abstract List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context);
    }
}
