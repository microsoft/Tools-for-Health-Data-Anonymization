// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class RedactProcessor : IAnonymizerProcessor
    {
        private RedactFunction _redactFunction;

        public RedactProcessor(JObject settingObject, IAnonymizerSettingsFactory settingFactory = null)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            settingFactory ??= new AnonymizerSettingsFactory();
            var redactSetting = settingFactory.CreateAnonymizerSetting<RedactSetting>(settingObject);
            _redactFunction = new RedactFunction(redactSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            var redactedValues = new List<string>();
            if (item.ValueRepresentation == DicomVR.AS)
            {
                var values = ((DicomAgeString)item).Get<string[]>();
                foreach (var value in values)
                {
                    var result = Utility.AgeToString(_redactFunction.RedactAge(Utility.ParseAge(value)));
                    if (result != null)
                    {
                        redactedValues.Add(result);
                    }
                }
            }
            else if (item.ValueRepresentation == DicomVR.DA)
            {
                var values = Utility.ParseDicomDate((DicomDate)item);
                foreach (var value in values)
                {
                    var result = _redactFunction.RedactDateTime(value);
                    if (result != null)
                    {
                        redactedValues.Add(Utility.GenerateDicomDateString((DateTimeOffset)result));
                    }
                }
            }
            else if (item.ValueRepresentation == DicomVR.DT)
            {
                var values = Utility.ParseDicomDateTime((DicomDateTime)item);
                foreach (var value in values)
                {
                    var result = _redactFunction.RedactDateTime(value);
                    if (result != null)
                    {
                        redactedValues.Add(Utility.GenerateDicomDateTimeString(result));
                    }
                }
            }
            else if (item.ValueRepresentation == DicomVR.SQ)
            {
                dicomDataset.AddOrUpdate<DicomDataset>(DicomVR.SQ, item.Tag);
                return;
            }

            if (redactedValues.Count != 0)
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, redactedValues.ToArray());
            }
            else
            {
                dicomDataset.AddOrUpdate<string>(item.ValueRepresentation, item.Tag, values: null);
            }
        }

        public bool IsSupportedVR(DicomItem item)
        {
            return true;
        }
    }
}
