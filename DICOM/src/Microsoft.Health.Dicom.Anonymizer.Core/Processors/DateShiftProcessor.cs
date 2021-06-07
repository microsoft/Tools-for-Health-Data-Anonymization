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
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Model;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Dicom.DeID.SharedLib;
using Microsoft.Health.Dicom.DeID.SharedLib.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        private readonly DateShiftFunction _dateShiftFunction;
        private readonly DateShiftScope _dateShiftScope = DateShiftScope.SopInstance;

        public DateShiftProcessor(JObject settingObject, IAnonymizerSettingsFactory settingFactory = null)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            settingFactory ??= new AnonymizerSettingsFactory();
            var dateShiftSetting = settingFactory.CreateAnonymizerSetting<DateShiftSetting>(settingObject);
            _dateShiftFunction = new DateShiftFunction(dateShiftSetting);
            if (settingObject.TryGetValue("DateShiftScope", StringComparison.OrdinalIgnoreCase, out JToken scope))
            {
                _dateShiftScope = (DateShiftScope)Enum.Parse(typeof(DateShiftScope), scope.ToString());
            }
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));
            EnsureArg.IsNotNull(context, nameof(context));

            _dateShiftFunction.DateShiftKeyPrefix = _dateShiftScope switch
            {
                DateShiftScope.StudyInstance => context.StudyInstanceUID ?? string.Empty,
                DateShiftScope.SeriesInstance => context.StudyInstanceUID ?? string.Empty,
                DateShiftScope.SopInstance => context.SopInstanceUID ?? string.Empty,
                _ => string.Empty,
            };
            if (item.ValueRepresentation == DicomVR.DA)
            {
                var values = Utility.ParseDicomDate((DicomDate)item).Select(_dateShiftFunction.ShiftDateTime).Where(x => !DateTimeUtility.IndicateAgeOverThreshold(x));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(Utility.GenerateDicomDateString).ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.DT)
            {
                var values = Utility.ParseDicomDateTime((DicomDateTime)item);
                var results = new List<string>();
                foreach (var dateObject in values)
                {
                    dateObject.DateValue = _dateShiftFunction.ShiftDateTime(dateObject.DateValue);
                    if (!DateTimeUtility.IndicateAgeOverThreshold(dateObject.DateValue))
                    {
                        results.Add(Utility.GenerateDicomDateTimeString(dateObject));
                    }
                }

                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, results.ToArray());
            }
            else
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"DateShift is not supported for {item.ValueRepresentation}");
            }
        }

        public bool IsSupportedVR(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            var supportedVR = Enum.GetNames(typeof(DateShiftSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code);
        }
    }
}
