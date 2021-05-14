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

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        private readonly DicomDateShiftSetting _defaultSetting;

        public DateShiftProcessor(DicomDateShiftSetting defaultSetting)
        {
            _defaultSetting = defaultSetting;
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, DicomBasicInformation basicInfo, IDicomAnonymizationSetting settings = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));
            EnsureArg.IsNotNull(basicInfo, nameof(basicInfo));

            if (!IsValidItemForDateShift(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Dateshift is not supported for {item.ValueRepresentation}");
            }

            var dateShiftSetting = (DicomDateShiftSetting)(settings ?? _defaultSetting);
            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting()
            {
                DateShiftRange = dateShiftSetting.DateShiftRange,
                DateShiftKey = dateShiftSetting.DateShiftKey,
            })
            {
                DateShiftKeyPrefix = dateShiftSetting.DateShiftScope switch
                {
                    DateShiftScope.StudyInstance => basicInfo.StudyInstanceUID ?? string.Empty,
                    DateShiftScope.SeriesInstance => basicInfo.StudyInstanceUID ?? string.Empty,
                    DateShiftScope.SopInstance => basicInfo.SopInstanceUID ?? string.Empty,
                    _ => string.Empty,
                },
            };
            if (item.ValueRepresentation == DicomVR.DA)
            {
                var values = ((DicomDate)item).Get<string[]>().Select(x => dateShiftFunction.ShiftDateTime(Utility.ParseDicomDate(x))).Where(x => !DateTimeUtility.IndicateAgeOverThreshold(x));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(x => Utility.GenerateDicomDateString(x)).ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.DT)
            {
                var results = new List<string>();
                var values = (item as DicomDateTime).Get<string[]>().ToList();
                foreach (var value in values)
                {
                    var dateObject = Utility.ParseDicomDateTime(value);
                    dateObject.DateValue = dateShiftFunction.ShiftDateTime(dateObject.DateValue);
                    if (!DateTimeUtility.IndicateAgeOverThreshold(dateObject.DateValue))
                    {
                        results.Add(Utility.GenerateDicomDateTimeString(dateObject));
                    }
                }

                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, results.ToArray());
            }
        }

        public bool IsValidItemForDateShift(DicomItem item)
        {
            var supportedVR = Enum.GetNames(typeof(DateShiftSupportedVR)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            return supportedVR.Contains(item.ValueRepresentation.Code);
        }
    }
}
