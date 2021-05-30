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
        private readonly DicomDateShiftSetting _ruleSetting;

        public DateShiftProcessor(IDicomAnonymizationSetting ruleSetting = null)
        {
            _ruleSetting = (DicomDateShiftSetting)(ruleSetting ?? new DicomDateShiftSetting());
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));
            EnsureArg.IsNotNull(context, nameof(context));

            /*
            Type[] tps = new Type[2];
            tps[0] = typeof(DicomTag);
            tps[1] = typeof(string);
            var test = item.GetType().GetConstructor(tps);
            object[] obj = new object[2];
            obj[0] = item.Tag;
            obj[1] = (object)("123".ToArray());
            var t = test.Invoke(obj);
            */

            if (!IsValidItemForDateShift(item))
            {
                throw new AnonymizationOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationFunction, $"Dateshift is not supported for {item.ValueRepresentation}");
            }

            var dateShiftFunction = new DateShiftFunction(new DateShiftSetting()
            {
                DateShiftRange = _ruleSetting.DateShiftRange,
                DateShiftKey = _ruleSetting.DateShiftKey,
            })
            {
                DateShiftKeyPrefix = _ruleSetting.DateShiftScope switch
                {
                    DateShiftScope.StudyInstance => context.StudyInstanceUID ?? string.Empty,
                    DateShiftScope.SeriesInstance => context.StudyInstanceUID ?? string.Empty,
                    DateShiftScope.SopInstance => context.SopInstanceUID ?? string.Empty,
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
